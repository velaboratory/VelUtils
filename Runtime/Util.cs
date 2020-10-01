using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace unityutilities
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class ReadOnlyAttribute : PropertyAttribute { }
#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyAttributeDrawer : UnityEditor.PropertyDrawer
	{
		public override void OnGUI(Rect rect, UnityEditor.SerializedProperty prop, GUIContent label)
		{
			bool wasEnabled = GUI.enabled;
			GUI.enabled = false;
			UnityEditor.EditorGUI.PropertyField(rect, prop);
			GUI.enabled = wasEnabled;
		}
	}
#endif

	public static class Extensions
	{
		#region Binary writer extensions
		public static void Write(this BinaryWriter writer, Vector2 vec2)
		{
			writer.Write(vec2.x);
			writer.Write(vec2.y);
		}

		public static void Write(this BinaryWriter writer, Vector3 vec3)
		{
			writer.Write(vec3.x);
			writer.Write(vec3.y);
			writer.Write(vec3.z);
		}

		public static void Write(this BinaryWriter writer, Quaternion quat)
		{
			writer.Write(quat.x);
			writer.Write(quat.y);
			writer.Write(quat.z);
			writer.Write(quat.w);
		}
		#endregion

		#region Binary reader extensions
		public static Vector2 ReadVector2(this BinaryReader reader)
		{
			return new Vector2
			{
				x = reader.ReadSingle(),
				y = reader.ReadSingle()
			};
		}

		public static Vector3 ReadVector3(this BinaryReader reader)
		{
			return new Vector3
			{
				x = reader.ReadSingle(),
				y = reader.ReadSingle(),
				z = reader.ReadSingle()
			};
		}

		public static Quaternion ReadQuaternion(this BinaryReader reader)
		{
			return new Quaternion
			{
				x = reader.ReadSingle(),
				y = reader.ReadSingle(),
				z = reader.ReadSingle(),
				w = reader.ReadSingle(),
			};
		}
		#endregion
	}

	public static class TransformDeepChildExtension
	{
		//Breadth-first search
		public static Transform FindDeepChild(this Transform aParent, string aName)
		{
			Queue<Transform> queue = new Queue<Transform>();
			queue.Enqueue(aParent);
			while (queue.Count > 0)
			{
				var c = queue.Dequeue();
				if (c.name == aName)
					return c;
				foreach (Transform t in c)
					queue.Enqueue(t);
			}
			return null;
		}
	}

	public static class Util
	{
		public static float map(float input, float in_min, float in_max, float out_min, float out_max)
		{
			var output = input;
			output -= in_min;
			output /= in_max - in_min;
			output *= out_max - out_min;
			output += out_min;
			return output;
		}

		private class AngleAndHit
		{
			public float angle;
			public RaycastHit hit;

			public AngleAndHit(float angle, RaycastHit hit)
			{
				this.angle = angle;
				this.hit = hit;
			}
		}

		/// <summary>
		/// Works with a SphereCast internally. The angle of the cone is determined by arctan(maxDistance/maxRadius)
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="direction"></param>
		/// <param name="hitInfo"></param>
		/// <param name="maxRadius"></param>
		/// <param name="maxDistance"></param>
		/// <returns>Hit or no?</returns>
		public static bool ConeCast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxRadius, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
		{
			RaycastHit[] hits = Physics.SphereCastAll(origin - Vector3.forward * maxRadius, maxRadius, direction, maxDistance, layerMask, queryTriggerInteraction);
			List<AngleAndHit> coneCastHitList = new List<AngleAndHit>();
			foreach (var hit in hits)
			{
				coneCastHitList.Add(new AngleAndHit(Vector3.Angle(direction, hit.point - origin), hit));
			}

			float angle = Mathf.Atan(maxRadius / maxDistance) * Mathf.Rad2Deg;

			coneCastHitList.RemoveAll(h => h.angle > angle);
			coneCastHitList.Sort((h1, h2) => h1.angle.CompareTo(h2.angle));

			if (coneCastHitList.Count > 0)
			{
				hitInfo = coneCastHitList.First().hit;
				return true;
			}

			hitInfo = new RaycastHit();
			return false;
		}

		/// <summary>
		/// Works with a SphereCast internally. The angle of the cone is determined by arctan(maxDistance/maxRadius)
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="direction"></param>
		/// <param name="hitInfo"></param>
		/// <param name="maxRadius"></param>
		/// <param name="maxDistance"></param>
		/// <returns>Hit or no?</returns>
		public static bool ConeCast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxRadius, float maxDistance)
		{
			return ConeCast(origin, direction, out hitInfo, maxRadius, maxDistance, ~0, QueryTriggerInteraction.UseGlobal);
		}

		/// <summary>
		/// Works with a SphereCast internally. The angle of the cone is determined by arctan(maxDistance/maxRadius)
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="direction"></param>
		/// <param name="maxRadius"></param>
		/// <param name="maxDistance"></param>
		/// <returns>Hit or no?</returns>
		public static RaycastHit[] ConeCastAll2(Vector3 origin, Vector3 direction, float maxRadius, float maxDistance)
		{
			RaycastHit[] hits = Physics.SphereCastAll(origin - Vector3.forward * maxRadius, maxRadius, direction, maxDistance);
			List<RaycastHit> coneCastHitList = new List<RaycastHit>();

			float angle = Mathf.Atan(maxRadius / maxDistance) * Mathf.Rad2Deg;

			foreach (var hit in hits)
			{
				float angleToHit = Vector3.Angle(direction, hit.point - origin);

				if (angleToHit < angle)
				{
					coneCastHitList.Add(hit);
				}
			}

			return coneCastHitList.ToArray();
		}

		/// <summary>
		/// Works with multiple progressively-larger SphereCasts internally. The angle of the cone is determined by arctan(maxDistance/maxRadius)
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="direction"></param>
		/// <param name="maxRadius"></param>
		/// <param name="maxDistance"></param>
		/// <returns>Hit or no?</returns>
		public static RaycastHit[] ConeCastAll(Vector3 origin, Vector3 direction, float maxRadius, float maxDistance, int layerMask=~0)
		{
			// get hits
			RaycastHit[] hits = Physics.SphereCastAll(origin - direction * maxRadius, maxRadius, direction, maxDistance, layerMask);
			List<RaycastHit> coneCastHitList = new List<RaycastHit>();

			float angle = Mathf.Atan(maxRadius / maxDistance) * Mathf.Rad2Deg;

			foreach (var hit in hits)
			{
				float angleToHit = Vector3.Angle(direction, hit.collider.transform.position - origin);

				if (angleToHit < angle)
				{
					coneCastHitList.Add(hit);
				}
			}

			return coneCastHitList.ToArray();
		}
	}
}
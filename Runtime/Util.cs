using System;
using System.IO;
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
}
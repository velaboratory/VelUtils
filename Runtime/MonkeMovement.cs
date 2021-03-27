using UnityEngine;

namespace GorillaLocomotion
{
	public class MonkeMovement : MonoBehaviour
	{
		private static MonkeMovement _instance;
		public SphereCollider headCollider;
		public CapsuleCollider bodyCollider;
		public Transform leftHandFollower;
		public Transform rightHandFollower;
		public Transform rightHandTransform;
		public Transform leftHandTransform;
		private Vector3 lastLeftHandPosition;
		private Vector3 lastRightHandPosition;
		private Vector3 lastHeadPosition;
		private Rigidbody playerRigidBody;
		public int velocityHistorySize = 6;
		public float maxArmLength = 1.5f;
		public float unStickDistance = .5f;
		public float velocityLimit = .3f;
		public float maxJumpSpeed = 6.5f;
		public float jumpMultiplier = 1.1f;
		public float minimumRaycastDistance = 0.03f;
		public float defaultSlideFactor = 0.03f;
		public float defaultPrecision = 0.98f;
		private Vector3[] velocityHistory;
		private int velocityIndex;
		private Vector3 currentVelocity;
		private Vector3 denormalizedVelocityAverage;
		private bool jumpHandIsLeft;
		private Vector3 lastPosition;
		public Vector3 rightHandOffset;
		public Vector3 leftHandOffset;
		public LayerMask locomotionEnabledLayers;
		public bool wasLeftHandTouching;
		public bool wasRightHandTouching;
		public bool disableMovement;
		public bool didATurn;
		
		
		private void Awake()
		{
			InitializeValues();
		}

		public void InitializeValues()
		{
			playerRigidBody = GetComponent<Rigidbody>();
			velocityHistory = new Vector3[velocityHistorySize];
			lastLeftHandPosition = leftHandFollower.transform.position;
			lastRightHandPosition = rightHandFollower.transform.position;
			lastHeadPosition = headCollider.transform.position;
			velocityIndex = 0;
			lastPosition = transform.position;
		}

		private Vector3 CurrentLeftHandPosition()
		{
			if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
			{
				return PositionWithOffset(leftHandTransform, leftHandOffset);
			}
			return headCollider.transform.position + (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
		}

		private Vector3 CurrentRightHandPosition()
		{
			if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
			{
				return PositionWithOffset(rightHandTransform, rightHandOffset);
			}
			return headCollider.transform.position + (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized * maxArmLength;
		}

		private static Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
		{
			return transformToModify.position + transformToModify.rotation * offsetVector;
		}

		private void Update()
		{
			bool flag = false;
			bool flag2 = false;
			Vector3 vector;
			Vector3 a = Vector3.zero;
			Vector3 b = Vector3.zero;
			bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);
			Vector3 movementVector = CurrentLeftHandPosition() - lastLeftHandPosition + Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;
			Vector3 a2;
			if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, movementVector, defaultPrecision, out a2, true))
			{
				if (wasLeftHandTouching)
				{
					a = lastLeftHandPosition - CurrentLeftHandPosition();
				}
				else
				{
					a = a2 - CurrentLeftHandPosition();
				}
				playerRigidBody.velocity = Vector3.zero;
				flag = true;
			}
			movementVector = CurrentRightHandPosition() - lastRightHandPosition + Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;
			if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, movementVector, defaultPrecision, out a2, true))
			{
				if (wasRightHandTouching)
				{
					b = lastRightHandPosition - CurrentRightHandPosition();
				}
				else
				{
					b = a2 - CurrentRightHandPosition();
				}
				playerRigidBody.velocity = Vector3.zero;
				flag2 = true;
			}
			if ((flag || wasLeftHandTouching) && (flag2 || wasRightHandTouching))
			{
				vector = (a + b) / 2f;
			}
			else
			{
				vector = a + b;
			}

			if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, headCollider.transform.position + vector - lastHeadPosition, defaultPrecision, out a2, false))
			{
				vector = a2 - lastHeadPosition;
				if (Physics.Raycast(lastHeadPosition, headCollider.transform.position - lastHeadPosition + vector, out RaycastHit _, (headCollider.transform.position - lastHeadPosition + vector).magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
				{
					vector = lastHeadPosition - headCollider.transform.position;
				}
			}
			if (vector != Vector3.zero)
			{
				transform.position += vector;
			}
			lastHeadPosition = headCollider.transform.position;
			movementVector = CurrentLeftHandPosition() - lastLeftHandPosition;
			if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, movementVector, defaultPrecision, out a2, (!flag && !wasLeftHandTouching) || (!flag2 && !wasRightHandTouching)))
			{
				lastLeftHandPosition = a2;
				flag = true;
			}
			else
			{
				lastLeftHandPosition = CurrentLeftHandPosition();
			}
			movementVector = CurrentRightHandPosition() - lastRightHandPosition;
			if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, movementVector, defaultPrecision, out a2, (!flag && !wasLeftHandTouching) || (!flag2 && !wasRightHandTouching)))
			{
				lastRightHandPosition = a2;
				flag2 = true;
			}
			else
			{
				lastRightHandPosition = CurrentRightHandPosition();
			}
			StoreVelocities();
			if ((flag2 || flag) && !disableMovement && !didATurn && denormalizedVelocityAverage.magnitude > velocityLimit)
			{
				if (denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
				{
					playerRigidBody.velocity = denormalizedVelocityAverage.normalized * maxJumpSpeed;
				}
				else
				{
					playerRigidBody.velocity = jumpMultiplier * denormalizedVelocityAverage;
				}
			}
			if (flag && (CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentLeftHandPosition() - headCollider.transform.position, out RaycastHit _, (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
			{
				lastLeftHandPosition = CurrentLeftHandPosition();
				flag = false;
			}
			if (flag2 && (CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentRightHandPosition() - headCollider.transform.position, out RaycastHit _, (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
			{
				lastRightHandPosition = CurrentRightHandPosition();
				flag2 = false;
			}
			leftHandFollower.position = lastLeftHandPosition;
			rightHandFollower.position = lastRightHandPosition;
			wasLeftHandTouching = flag;
			wasRightHandTouching = flag2;
		}

		// Token: 0x0600053E RID: 1342 RVA: 0x00024BCC File Offset: 0x00022DCC
		private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand)
		{
			RaycastHit raycastHit;
			if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out raycastHit))
			{
				Vector3 vector = endPosition;
				Surface component = raycastHit.collider.GetComponent<Surface>();
				float d = (component != null) ? component.slipPercentage : ((!singleHand) ? defaultSlideFactor : 0.001f);
				Vector3 vector2 = Vector3.ProjectOnPlane(startPosition + movementVector - vector, raycastHit.normal) * d;
				if (CollisionsSphereCast(endPosition, sphereRadius, vector2, precision * precision, out endPosition, out raycastHit))
				{
					return true;
				}
				if (CollisionsSphereCast(vector2 + vector, sphereRadius, startPosition + movementVector - (vector2 + vector), precision * precision * precision, out endPosition, out raycastHit))
				{
					return true;
				}
				endPosition = vector;
				return true;
			}
			else
			{
				if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f, movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f, out endPosition, out raycastHit))
				{
					endPosition = startPosition;
					return true;
				}
				endPosition = Vector3.zero;
				return false;
			}
		}

		// Token: 0x0600053F RID: 1343 RVA: 0x00024CF8 File Offset: 0x00022EF8
		private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
		{
			if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * (1f - precision), locomotionEnabledLayers.value))
			{
				finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;
				RaycastHit raycastHit;
				if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out raycastHit, (finalPosition - startPosition).magnitude + sphereRadius * (1f - precision * precision), locomotionEnabledLayers.value))
				{
					finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0f, hitInfo.distance - sphereRadius * (1f - precision * precision));
					hitInfo = raycastHit;
				}
				else if (Physics.Raycast(startPosition, finalPosition - startPosition, out raycastHit, (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f, locomotionEnabledLayers.value))
				{
					finalPosition = startPosition;
					hitInfo = raycastHit;
					return true;
				}
				return true;
			}
			if (Physics.Raycast(startPosition, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
			{
				finalPosition = startPosition;
				return true;
			}
			finalPosition = Vector3.zero;
			return false;
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x00024E88 File Offset: 0x00023088
		public bool IsHandTouching(bool forLeftHand)
		{
			if (forLeftHand)
			{
				return wasLeftHandTouching;
			}
			return wasRightHandTouching;
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x00024E9C File Offset: 0x0002309C
		public void Turn(float degrees)
		{
			transform.RotateAround(headCollider.transform.position, transform.up, degrees);
			denormalizedVelocityAverage = Quaternion.Euler(0f, degrees, 0f) * denormalizedVelocityAverage;
			for (int i = 0; i < velocityHistory.Length; i++)
			{
				velocityHistory[i] = Quaternion.Euler(0f, degrees, 0f) * velocityHistory[i];
			}
		}

		// Token: 0x06000542 RID: 1346 RVA: 0x00024F34 File Offset: 0x00023134
		private void StoreVelocities()
		{
			velocityIndex = (velocityIndex + 1) % velocityHistorySize;
			Vector3 b = velocityHistory[velocityIndex];
			currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
			denormalizedVelocityAverage += (currentVelocity - b) / (float)velocityHistorySize;
			velocityHistory[velocityIndex] = currentVelocity;
			lastPosition = transform.position;
		}
	}
}

﻿using System;
using System.Linq;
using UnityEngine;

namespace GorillaLocomotion
{
	[AddComponentMenu("Gorilla Locomotion/Gorilla Player")]
	public class GorillaPlayer : MonoBehaviour
	{
		private static GorillaPlayer _instance;
		public static GorillaPlayer Instance => _instance;

		[Header("Head/Hand references")] public SphereCollider headCollider;
		public CapsuleCollider bodyCollider;

		public Transform leftHandTransform;
		public Transform rightHandTransform;

		// These objects follow the tracked hands, but don't go through objects based on physics calculated here.
		[Header("Hand Followers (for graphics)")]
		public Transform leftHandFollower;

		public Transform rightHandFollower;

		[Header("Sounds")] public AudioSource leftHandTouchSource;
		public AudioSource rightHandTouchSource;

		private Vector3 lastLeftHandPosition;
		private Vector3 lastRightHandPosition;
		private Vector3 lastHeadPosition;

		private Rigidbody playerRigidBody;

		[Header("Config Tweaks")] public int velocityHistorySize = 6;
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

		public LayerMask locomotionEnabledLayers = ~4;

		private bool wasLeftHandTouching;
		private bool wasRightHandTouching;
		private float leftHandLastTouchTime;
		private float rightHandLastTouchTime;

		/// <summary>
		/// isRight, position, velocity
		/// </summary>
		public Action<bool, Vector3, Vector3> HandTouchEnter;

		/// <summary>
		/// isRight, position, velocity
		/// </summary>
		public Action<bool, Vector3, Vector3> HandTouchExit;

		public bool disableMovement = false;


		private void OnEnable()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
			}
			else
			{
				_instance = this;
			}

			InitializeValues();

			HandTouchEnter += OnHandTouchEnter;
		}

		private void OnDisable()
		{
			HandTouchEnter -= OnHandTouchEnter;
		}

		private void OnHandTouchEnter(bool isRight, Vector3 position, Vector3 velocity)
		{
			// Debug.Log(velocity.magnitude);

			if (isRight)
			{
				if (rightHandTouchSource != null)
				{
					rightHandTouchSource.Play();
				}
			}
			else
			{
				if (leftHandTouchSource != null)
				{
					leftHandTouchSource.Play();
				}
			}
		}

		private void InitializeValues()
		{
			playerRigidBody = GetComponent<Rigidbody>();
			velocityHistory = new Vector3[velocityHistorySize];
			lastLeftHandPosition = leftHandTransform.transform.position;
			lastRightHandPosition = rightHandTransform.transform.position;
			lastHeadPosition = headCollider.transform.position;
			velocityIndex = 0;
			lastPosition = transform.position;
			currentVelocity = Vector3.zero;
			denormalizedVelocityAverage = Vector3.zero;
			wasLeftHandTouching = false;
			wasRightHandTouching = false;
			leftHandLastTouchTime = 0;
			rightHandLastTouchTime = 0;
		}

		private Vector3 CurrentLeftHandPosition()
		{
			if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
			{
				return PositionWithOffset(leftHandTransform, leftHandOffset);
			}
			else
			{
				return headCollider.transform.position + (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
			}
		}

		private Vector3 CurrentRightHandPosition()
		{
			if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
			{
				return PositionWithOffset(rightHandTransform, rightHandOffset);
			}
			else
			{
				return headCollider.transform.position + (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized * maxArmLength;
			}
		}

		private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
		{
			return transformToModify.position + transformToModify.rotation * offsetVector;
		}

		private void Update()
		{
			bool leftHandColliding = false;
			bool rightHandColliding = false;
			Vector3 finalPosition;
			Vector3 rigidBodyMovement = Vector3.zero;
			Vector3 firstIterationLeftHand = Vector3.zero;
			Vector3 firstIterationRightHand = Vector3.zero;
			RaycastHit hitInfo;

			bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

			//left hand

			Vector3 distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition + Vector3.down * (2f * 9.8f * Time.deltaTime * Time.deltaTime);

			if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
			{
				//this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
				if (wasLeftHandTouching)
				{
					firstIterationLeftHand = lastLeftHandPosition - CurrentLeftHandPosition();
				}
				else
				{
					firstIterationLeftHand = finalPosition - CurrentLeftHandPosition();
					Vector3 velocity = distanceTraveled * Time.deltaTime;
					if (velocity.magnitude > .0001f && Time.time - leftHandLastTouchTime > .2f)
					{
						HandTouchEnter?.Invoke(false, CurrentLeftHandPosition(), velocity);
						leftHandLastTouchTime = Time.time;
					}
				}

				playerRigidBody.velocity = Vector3.zero;

				leftHandColliding = true;
			}

			//right hand

			distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition + Vector3.down * (2f * 9.8f * Time.deltaTime * Time.deltaTime);

			if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
			{
				if (wasRightHandTouching)
				{
					firstIterationRightHand = lastRightHandPosition - CurrentRightHandPosition();
				}
				else
				{
					firstIterationRightHand = finalPosition - CurrentRightHandPosition();
					Vector3 velocity = distanceTraveled * Time.deltaTime;
					if (velocity.magnitude > .0001f && Time.time - rightHandLastTouchTime > .2f)
					{
						HandTouchEnter?.Invoke(true, CurrentRightHandPosition(), distanceTraveled * Time.deltaTime);
						rightHandLastTouchTime = Time.time;
					}
				}

				playerRigidBody.velocity = Vector3.zero;

				rightHandColliding = true;
			}

			//average or add

			if ((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))
			{
				//this lets you grab stuff with both hands at the same time
				rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
			}
			else
			{
				rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
			}

			//check valid head movement

			if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, headCollider.transform.position + rigidBodyMovement - lastHeadPosition, defaultPrecision,
				    out finalPosition, false))
			{
				rigidBodyMovement = finalPosition - lastHeadPosition;
				//last check to make sure the head won't phase through geometry
				if (Physics.Raycast(lastHeadPosition, headCollider.transform.position - lastHeadPosition + rigidBodyMovement, out hitInfo,
					    (headCollider.transform.position - lastHeadPosition + rigidBodyMovement).magnitude + headCollider.radius * defaultPrecision * 0.999f,
					    locomotionEnabledLayers.value))
				{
					rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
				}
			}

			if (rigidBodyMovement != Vector3.zero)
			{
				transform.position = transform.position + rigidBodyMovement;
			}

			lastHeadPosition = headCollider.transform.position;

			//do final left hand position

			distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition;

			if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition,
				    !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
			{
				leftHandColliding = true;
				lastLeftHandPosition = finalPosition;
			}
			else
			{
				lastLeftHandPosition = CurrentLeftHandPosition();
			}

			//do final right hand position

			distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition;

			if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition,
				    !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
			{
				rightHandColliding = true;
				lastRightHandPosition = finalPosition;
			}
			else
			{
				lastRightHandPosition = CurrentRightHandPosition();
			}

			StoreVelocities();

			if ((rightHandColliding || leftHandColliding) && !disableMovement)
			{
				if (denormalizedVelocityAverage.magnitude > velocityLimit)
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
			}

			//check to see if left hand is stuck and we should unstick it

			if (leftHandColliding && (CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position,
				    minimumRaycastDistance * defaultPrecision, CurrentLeftHandPosition() - headCollider.transform.position, out hitInfo,
				    (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
			{
				lastLeftHandPosition = CurrentLeftHandPosition();
				leftHandColliding = false;
				HandTouchExit?.Invoke(false, CurrentLeftHandPosition(), (CurrentLeftHandPosition() - lastLeftHandPosition) * Time.deltaTime);
			}

			//check to see if right hand is stuck and we should unstick it

			if (rightHandColliding && (CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position,
				    minimumRaycastDistance * defaultPrecision, CurrentRightHandPosition() - headCollider.transform.position, out hitInfo,
				    (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
			{
				lastRightHandPosition = CurrentRightHandPosition();
				rightHandColliding = false;
				HandTouchExit?.Invoke(true, CurrentRightHandPosition(), (CurrentRightHandPosition() - lastRightHandPosition) * Time.deltaTime);
			}

			// update the hand followers for visual output
			if (leftHandFollower != null)
			{
				leftHandFollower.position = lastLeftHandPosition;
			}

			if (rightHandFollower != null)
			{
				rightHandFollower.position = lastRightHandPosition;
			}


			wasLeftHandTouching = leftHandColliding;
			wasRightHandTouching = rightHandColliding;
		}

		private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand)
		{
			RaycastHit hitInfo;
			Vector3 movementToProjectedAboveCollisionPlane;
			GorillaSurface gorillaSurface;
			float slipPercentage;
			//first spherecast from the starting position to the final position
			if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out hitInfo))
			{
				//if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

				//take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance away to account for moving perpendicular to that surface
				Vector3 firstPosition = endPosition;
				gorillaSurface = hitInfo.collider.GetComponent<GorillaSurface>();
				slipPercentage = gorillaSurface != null ? gorillaSurface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
				movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
				if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision, out endPosition, out hitInfo))
				{
					//if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
					return true;
				}
				//if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface
				else if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius,
					         startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition), precision * precision * precision, out endPosition,
					         out hitInfo))
				{
					//if we hit, then return the spot we hit
					return true;
				}
				else
				{
					//this shouldn't really happe, since this means that the sliding motion got you around some corner or something and let you get to your final point. back off because something strange happened, so just don't do the slide
					endPosition = firstPosition;
					return true;
				}
			}
			//as kind of a sanity check, try a smaller spherecast. this accounts for times when the original spherecast was already touching a surface so it didn't trigger correctly
			else if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f,
				         movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f, out endPosition, out hitInfo))
			{
				endPosition = startPosition;
				return true;
			}
			else
			{
				endPosition = Vector3.zero;
				return false;
			}
		}

		private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
		{
			//kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
			//be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

			//initial spherecase
			RaycastHit innerHit;
			if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * (1 - precision),
				    locomotionEnabledLayers.value))
			{
				//if we hit, we're trying to move to a position a sphereradius distance from the normal
				finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

				//check a spherecase from the original position to the intended final position
				if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out innerHit,
					    (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision), locomotionEnabledLayers.value))
				{
					finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
					hitInfo = innerHit;
				}
				//bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
				else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit,
					         (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f, locomotionEnabledLayers.value))
				{
					finalPosition = startPosition;
					hitInfo = innerHit;
					return true;
				}

				return true;
			}
			//anti-clipping through geometry check
			else if (Physics.Raycast(startPosition, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
			{
				finalPosition = startPosition;
				return true;
			}
			else
			{
				finalPosition = Vector3.zero;
				return false;
			}
		}

		public bool IsHandTouching(bool forLeftHand)
		{
			if (forLeftHand)
			{
				return wasLeftHandTouching;
			}
			else
			{
				return wasRightHandTouching;
			}
		}

		public void Turn(float degrees)
		{
			transform.RotateAround(headCollider.transform.position, transform.up, degrees);
			denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
			for (int i = 0; i < velocityHistory.Length; i++)
			{
				velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
			}
		}

		private void StoreVelocities()
		{
			velocityIndex = (velocityIndex + 1) % velocityHistorySize;
			Vector3 oldestVelocity = velocityHistory[velocityIndex];
			currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
			denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
			Debug.Log($"{string.Join(", ", velocityHistory.Select(v => v.magnitude))}", this);
			velocityHistory[velocityIndex] = currentVelocity;
			lastPosition = transform.position;
		}
	}
}
using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
	private readonly string Axis_MouseScrollWheel = "Mouse ScrollWheel";
	private readonly string Axis_Horizontal = "Horizontal";
	private readonly string Axis_Vertical = "Vertical";
	public Transform target;
	public float rotateSpeed = 1f;
	public float transitionSpeed;

	public bool invertHoriz;
	public bool invertVertical;

	private Transform prevTarget;
	private bool changingTarget;
	private Quaternion targetRot;
	private Quaternion prevRot;

	private bool movingToPoint;
	private Vector3 targetPos;

	private float t;
	private float maxZoomOut = 5f;

	private float targetAngle;

	void Start()
	{
		transform.LookAt(target);
		prevTarget = target;
	}

	void LateUpdate()
	{
		if (changingTarget)
		{
			t += Time.deltaTime * transitionSpeed;
			transform.rotation = Quaternion.Slerp(prevRot, targetRot, t);

			if (target.position == Vector3.zero)
			{
				//Zoom out
				Camera.main.fieldOfView = Camera.main.fieldOfView + Time.deltaTime * transitionSpeed * 3f;
				Camera.main.orthographicSize = Camera.main.orthographicSize + Time.deltaTime * transitionSpeed * 3f;
				Vector3 normalizedTransform = transform.position;
				normalizedTransform = normalizedTransform.normalized * 15f;
				transform.position = normalizedTransform;
			}
			else
			{
				//Zoom in
				Camera.main.fieldOfView = Camera.main.fieldOfView - Time.deltaTime * transitionSpeed * 3f;
				Camera.main.orthographicSize = Camera.main.orthographicSize - Time.deltaTime * transitionSpeed * 3f;
			}
			if (Camera.main.orthographicSize > maxZoomOut)
			{
				Camera.main.orthographicSize = maxZoomOut;
			}
			if (Camera.main.orthographicSize < 1.5f)
			{
				Camera.main.orthographicSize = 1.5f;
			}
			if (t >= 1)
			{
				changingTarget = false;
				prevTarget = target;
			}
			return;
		}
		if (movingToPoint)
		{
			//t += Time.deltaTime * transitionSpeed * .3f;
			targetAngle -= Time.deltaTime * transitionSpeed * 25f;
			//Debug.Log("Current Angle: " + targetAngle);
			transform.position = Vector3.RotateTowards(transform.position, targetPos, (Time.deltaTime * transitionSpeed * 25f) * Mathf.Deg2Rad, 0f);
			transform.LookAt(Vector3.zero);

			if (targetAngle <= 0)
			{
				movingToPoint = false;
			}
			return;
		}

		float axis_mouseScrollwheel = Input.GetAxis(Axis_MouseScrollWheel);
		if (axis_mouseScrollwheel < 0)
		{
			Camera.main.fieldOfView = Camera.main.fieldOfView + 5f;
			Camera.main.orthographicSize = Camera.main.orthographicSize + 1f;
		}
		if (axis_mouseScrollwheel > 0)
		{
			Camera.main.fieldOfView = Camera.main.fieldOfView - 2.5f;
			Camera.main.orthographicSize = Camera.main.orthographicSize - 1f;
			if (Camera.main.orthographicSize < 1.5f)
			{
				Camera.main.orthographicSize = 1.5f;
			}
			if (Camera.main.fieldOfView < 5)
			{
				Camera.main.fieldOfView = 5f;
			}
		}

		float axis_horizontal = Input.GetAxis(Axis_Horizontal);
		if (axis_horizontal > 0)
		{
			transform.RotateAround(target.position, transform.up, rotateSpeed * (invertHoriz ? -1f : 1f));
		}
		if (axis_horizontal < 0)
		{
			transform.RotateAround(target.position, transform.up, -rotateSpeed * (invertHoriz ? -1f : 1f));
		}
		float axis_vertical = Input.GetAxis(Axis_Vertical);
		if (axis_vertical > 0)
		{
			transform.RotateAround(target.position, transform.right, -rotateSpeed * (invertVertical ? -1f : 1f));
		}
		if (axis_vertical < 0)
		{
			transform.RotateAround(target.position, transform.right, rotateSpeed * (invertVertical ? -1f : 1f));
		}
	}

	/// <summary>
	/// Moves the camera to focus over a certain point on the surface of the sphere.
	/// </summary>
	/// <param name="pos">Position on sphere to move to.</param>
	public void moveToPointOnSphere(Vector3 pos)
	{
		if (target.position != Vector3.zero)
		{
			//setTarget(Vector3.zero);		
		}
		targetAngle = Vector3.Angle(transform.position, pos);

		t = targetAngle * Time.deltaTime * (1f / transitionSpeed);
		prevRot = transform.rotation;
		targetPos = pos;
		//t = 0f;
		movingToPoint = true;
	}

	/// <summary>
	/// Sets the new rotational center of camera to focus on different celestial body.
	/// </summary>
	/// <param name="pos">Position of new rotational center.</param>
	public void setTarget(Transform newTarget)
	{
		target = newTarget;
		moveToNewTarget();
	}

	private void moveToNewTarget()
	{
		if (target != prevTarget)
		{
			//PlayerUI.instance.showCamResetButton();
			Vector3 dir = target.position - transform.position;
			targetRot = Quaternion.LookRotation(dir, transform.up);
			prevRot = transform.rotation;
			t = 0f;
			changingTarget = true;
		}
	}
}

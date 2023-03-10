using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WireBoxPuzzle : Puzzle
{
	[SerializeField] private List<GameObject> offObjects;
	[SerializeField] private Camera raycastCamera;
	[SerializeField] private LayerMask puzzleWallLayer;
	[SerializeField] private UnityEvent onPuzzleComplete;
	[SerializeField] private UnityEvent OnCollide;
	
	[SerializeField] private Button backButton;
	[SerializeField] private Animator doorsAnimator;
	[SerializeField] private GameObject tutorial;
	
	[SerializeField] private UnityEvent onPlayAnimation;
	[SerializeField] private GameObject frontView;
	[SerializeField] private GameObject upView;
	[SerializeField] private WirePart wirePrefab;
	[SerializeField] private GameObject startWirePoint;
	[SerializeField] private GameObject targetWirePoint;
	[SerializeField] private GameObject targetSphere;
	[SerializeField] private float checkPuzzleDistance = 0.1f;
	[SerializeField] private GameObject debug;
	[SerializeField] private Collider backWall;
	[SerializeField] private Collider downWall;
	private WirePart _lastPart;
	private readonly List<WirePart> _parts = new List<WirePart>();

	private bool _cameraSwapped;

	private bool _allowInput;
	private bool _cameraMove;
	private static readonly int Open = Animator.StringToHash("Open");

	private void Awake()
	{
		backButton.onClick.AddListener(OnBackButtonClick);
	}
	
	private void OnEnable()
	{
		foreach (GameObject offObject in offObjects)
		{
			offObject.gameObject.SetActive(false);
		}
		StartCoroutine(StartPuzzleCor());
	}

	private void OnDisable()
	{
		foreach (GameObject offObject in offObjects)
		{
			if (offObject != null)
			{
				offObject.SetActive(true);
			}
		}
	}

	private void Update()
	{
		if (!_allowInput)
			return;
		if (_cameraMove)
			return;
		
		if (Input.GetMouseButtonDown(1))
		{
			SwapCamera();
		}
		if (_lastPart == null)
			return;
		
		if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			AddWirePart();
		}

		UpdateWallColliders();
		UpdatePartInput();
	}

	private void AddWirePart()
	{
		if (_lastPart.IsCollide)
		{
			OnCollide.Invoke();
			DestroyWires();
			//Debug.Log("OOOPS");
			InitializePuzzle();
		}
		else
		{
			var distanceToTarget = (_lastPart.mountPoint.position - targetWirePoint.transform.position).magnitude;
			if (distanceToTarget < checkPuzzleDistance)
			{
				_allowInput = false;
				//Debug.Log(distanceToTarget);
				_lastPart.Freeze();
				var len = (_lastPart.transform.position - targetWirePoint.transform.position);
				_lastPart.transform.forward = len.normalized * -1;
				var s = _lastPart.transform.localScale;
				var scaleFactor = 0.2f;
				_lastPart.transform.localScale = new Vector3(s.x,s.y,len.magnitude/scaleFactor);
				targetSphere.gameObject.SetActive(true);
				StartCoroutine(CompletePuzzle());
			}
			else
			{
				var part = Instantiate(wirePrefab, _lastPart.mountPoint);
				_lastPart = part;
				_parts.Add(part);
			}
		}
	}

	private void InitializePuzzle()
	{
		var part = Instantiate(wirePrefab, startWirePoint.transform);
		_lastPart = part;
		_parts.Add(part);
	}

	private void DestroyWires()
	{
		foreach (var wirePart in _parts)
		{
			Destroy(wirePart.gameObject);
		}
		_parts.Clear();
		_lastPart = null;
	}

	private void UpdateWallColliders()
	{
		if (_lastPart == null)
			return;
		if (_cameraSwapped)
		{
			backWall.gameObject.SetActive(false);
			downWall.gameObject.SetActive(true);
			var dPosition = downWall.transform.position;
			dPosition.y = _lastPart.transform.position.y;
			downWall.transform.position = dPosition;
		}
		else
		{
			backWall.gameObject.SetActive(true);
			downWall.gameObject.SetActive(false);
			var bPosition = backWall.transform.position;
			bPosition.z = _lastPart.transform.position.z;
			backWall.transform.position = bPosition;
		}
	}

	private void UpdatePartInput()
	{
		_lastPart.UpdateInput(raycastCamera, _cameraSwapped, puzzleWallLayer);
		debug.transform.position = _lastPart.cursor;
	}

	private void SwapCamera()
	{
		if (_cameraSwapped)
		{
			StartCoroutine(SwapCameraFront());
		}
		else
		{
			StartCoroutine(SwapCameraUp());
		}
	}

	private IEnumerator SwapCameraFront()
	{
		_cameraMove = true;
		raycastCamera.transform.DORotate(frontView.transform.eulerAngles, 1f).SetEase(Ease.InOutSine);
		yield return raycastCamera.transform.DOMove(frontView.transform.position, 1f).SetEase(Ease.InOutSine);
		_cameraSwapped = false;
		_cameraMove = false;
	}
	
	private IEnumerator SwapCameraUp()
	{
		_cameraMove = true;
		raycastCamera.transform.DORotate(upView.transform.eulerAngles, 1f).SetEase(Ease.InOutSine);
		yield return raycastCamera.transform.DOMove(upView.transform.position, 1f).SetEase(Ease.InOutSine);
		_cameraSwapped = true;
		_cameraMove = false;
	}

	private IEnumerator StartPuzzleCor()
	{
		yield return new WaitForSeconds(0.5f);
		onPlayAnimation.Invoke();
		doorsAnimator.SetBool(Open, true);
		yield return new WaitForSeconds(0.5f);
		tutorial.gameObject.SetActive(true);
		InitializePuzzle();
		_allowInput = true;
	}

	private IEnumerator CompletePuzzle()
	{
		_allowInput = false;
		if (_cameraSwapped)
		{
			_cameraSwapped = false;
			yield return SwapCameraFront();
		}
		
		onPuzzleComplete.Invoke();
		tutorial.gameObject.SetActive(false);
		yield return new WaitForSeconds(0.5f);
		onPlayAnimation.Invoke();
		doorsAnimator.SetBool(Open, false);
		yield return new WaitForSeconds(0.7f);
		state.Set(EState.Complete);
		gameObject.SetActive(false);
		DestroyWires();
	}

	private IEnumerator StopPuzzleCor()
	{
		_allowInput = false;
		if (_cameraSwapped)
		{
			_cameraSwapped = false;
			yield return SwapCameraFront();
		}
		
		tutorial.gameObject.SetActive(false);
		yield return new WaitForSeconds(0.2f);
		onPlayAnimation.Invoke();
		doorsAnimator.SetBool(Open, false);
		gameObject.SetActive(false);
		DestroyWires();
	}

	private void OnBackButtonClick()
	{
		StartCoroutine(StopPuzzleCor());
	}
}
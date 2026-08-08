using UnityEngine;
using System.Collections;
using NAN2026.Gomoku;


namespace TMPro.Examples
{
    
    public class CameraController : MonoBehaviour
    {
        public enum CameraModes { Follow, Isometric, Free }

        private Transform cameraTransform;
        private Transform dummyTarget;

        public Transform CameraTarget;

        public float FollowDistance = 30.0f;
        public float MaxFollowDistance = 100.0f;
        public float MinFollowDistance = 2.0f;

        public float ElevationAngle = 30.0f;
        public float MaxElevationAngle = 85.0f;
        public float MinElevationAngle = 0f;

        public float OrbitalAngle = 0f;

        public CameraModes CameraMode = CameraModes.Follow;

        public bool MovementSmoothing = true;
        public bool RotationSmoothing = false;
        private bool previousSmoothing;

        public float MovementSmoothingValue = 25f;
        public float RotationSmoothingValue = 5.0f;

        public float MoveSensitivity = 2.0f;

        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 desiredPosition;
        private float mouseX;
        private float mouseY;
        private Vector3 moveVector;
        private float mouseWheel;

        // Controls for Touches on Mobile devices
        //private float prev_ZoomDelta;


        private const string event_SmoothingValue = "Slider - Smoothing Value";
        private const string event_FollowDistance = "Slider - Camera Zoom";

        [Header("Gomoku Combat Framing")]
        [SerializeField] private bool useGomokuUiFraming;
        [SerializeField] private GomokuHud gomokuHud;
        [SerializeField, Min(0f)] private float combatHorizontalPadding = 42f;
        [SerializeField, Min(0f)] private float combatBottomPadding = 26f;
        [SerializeField, Min(0f)] private float turnUiPadding = 22f;

        private GomokuHud subscribedHud;
        private RectTransform boardRect;
        private RectTransform turnStatusRect;
        private Vector2 placementBoardPosition;
        private Vector2 placementBoardSize;
        private Coroutine framingRoutine;
        private bool placementFrameCached;


        void Awake()
        {
            cameraTransform = transform;
            previousSmoothing = MovementSmoothing;

            if (useGomokuUiFraming)
            {
                InitializeGomokuFraming();
                return;
            }

            if (QualitySettings.vSyncCount > 0)
                Application.targetFrameRate = 60;
            else
                Application.targetFrameRate = -1;

            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
                Input.simulateMouseWithTouches = false;
        }

        void OnEnable()
        {
            if (useGomokuUiFraming)
            {
                InitializeGomokuFraming();
            }
        }

        void OnDisable()
        {
            if (framingRoutine != null)
            {
                StopCoroutine(framingRoutine);
                framingRoutine = null;
            }

            UnsubscribeFromHud();
        }

        // Use this for initialization
        void Start()
        {
            if (useGomokuUiFraming)
            {
                InitializeGomokuFraming();
                return;
            }

            if (CameraTarget == null)
            {
                // If we don't have a target (assigned by the player, create a dummy in the center of the scene).
                dummyTarget = new GameObject("Camera Target").transform;
                CameraTarget = dummyTarget;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (useGomokuUiFraming)
            {
                return;
            }

            GetPlayerInput();


            // Check if we still have a valid target
            if (CameraTarget != null)
            {
                if (CameraMode == CameraModes.Isometric)
                {
                    desiredPosition = CameraTarget.position + Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0, 0, -FollowDistance);
                }
                else if (CameraMode == CameraModes.Follow)
                {
                    desiredPosition = CameraTarget.position + CameraTarget.TransformDirection(Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * (new Vector3(0, 0, -FollowDistance)));
                }
                else
                {
                    // Free Camera implementation
                }

                if (MovementSmoothing == true)
                {
                    // Using Smoothing
                    cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, desiredPosition, ref currentVelocity, MovementSmoothingValue * Time.fixedDeltaTime);
                    //cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5.0f);
                }
                else
                {
                    // Not using Smoothing
                    cameraTransform.position = desiredPosition;
                }

                if (RotationSmoothing == true)
                    cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, Quaternion.LookRotation(CameraTarget.position - cameraTransform.position), RotationSmoothingValue * Time.deltaTime);
                else
                {
                    cameraTransform.LookAt(CameraTarget);
                }

            }

        }



        private void InitializeGomokuFraming()
        {
            if (gomokuHud == null)
            {
                gomokuHud = FindFirstObjectByType<GomokuHud>();
            }

            if (gomokuHud == null)
            {
                Debug.LogWarning(
                    "Gomoku camera framing requires a GomokuHud in the scene.",
                    this);
                return;
            }

            if (subscribedHud != gomokuHud)
            {
                UnsubscribeFromHud();
                subscribedHud = gomokuHud;
                subscribedHud.CameraFramingRequested += HandleCameraFramingRequested;
            }

            boardRect = gomokuHud.BoardRect;
            turnStatusRect = gomokuHud.TurnStatusRect;
            if (!placementFrameCached && boardRect != null)
            {
                placementBoardPosition = boardRect.anchoredPosition;
                placementBoardSize = boardRect.sizeDelta;
                placementFrameCached = true;
            }
        }

        private void UnsubscribeFromHud()
        {
            if (subscribedHud == null)
            {
                return;
            }

            subscribedHud.CameraFramingRequested -= HandleCameraFramingRequested;
            subscribedHud = null;
        }

        private void HandleCameraFramingRequested(
            bool expanded,
            float duration,
            System.Action onCompleted)
        {
            InitializeGomokuFraming();
            if (!placementFrameCached || boardRect == null)
            {
                onCompleted?.Invoke();
                return;
            }

            if (framingRoutine != null)
            {
                StopCoroutine(framingRoutine);
            }

            Vector2 targetPosition = placementBoardPosition;
            Vector2 targetSize = placementBoardSize;
            if (expanded)
            {
                CalculateCombatFrame(out targetPosition, out targetSize);
            }

            bool alreadyAtTarget = (boardRect.anchoredPosition - targetPosition).sqrMagnitude
                    <= 0.0001f
                && (boardRect.sizeDelta - targetSize).sqrMagnitude <= 0.0001f;
            if (!isActiveAndEnabled || duration <= Mathf.Epsilon || alreadyAtTarget)
            {
                boardRect.anchoredPosition = targetPosition;
                boardRect.sizeDelta = targetSize;
                framingRoutine = null;
                onCompleted?.Invoke();
                return;
            }

            framingRoutine = StartCoroutine(AnimateFrame(
                targetPosition,
                targetSize,
                duration,
                onCompleted));
        }

        private IEnumerator AnimateFrame(
            Vector2 targetPosition,
            Vector2 targetSize,
            float duration,
            System.Action onCompleted)
        {
            Vector2 startPosition = boardRect.anchoredPosition;
            Vector2 startSize = boardRect.sizeDelta;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress < 0.5f
                    ? 4f * progress * progress * progress
                    : 1f - Mathf.Pow(-2f * progress + 2f, 3f) * 0.5f;
                boardRect.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    eased);
                boardRect.sizeDelta = Vector2.LerpUnclamped(
                    startSize,
                    targetSize,
                    eased);
                yield return null;
            }

            boardRect.anchoredPosition = targetPosition;
            boardRect.sizeDelta = targetSize;
            framingRoutine = null;
            onCompleted?.Invoke();
        }

        private void CalculateCombatFrame(
            out Vector2 targetPosition,
            out Vector2 targetSize)
        {
            targetPosition = placementBoardPosition;
            targetSize = placementBoardSize;

            RectTransform frameParent = boardRect.parent as RectTransform;
            if (frameParent == null || turnStatusRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            Rect parentRect = frameParent.rect;
            Bounds turnBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                frameParent,
                turnStatusRect);
            float availableBottom = parentRect.yMin + combatBottomPadding;
            float availableTop = Mathf.Min(
                parentRect.yMax - turnUiPadding,
                turnBounds.min.y - turnUiPadding);
            float availableWidth = parentRect.width - combatHorizontalPadding * 2f;
            float availableHeight = availableTop - availableBottom;
            float squareSize = Mathf.Min(availableWidth, availableHeight);
            if (squareSize <= Mathf.Epsilon)
            {
                return;
            }

            targetPosition.y = (availableBottom + availableTop) * 0.5f;
            targetSize = Vector2.one * squareSize;
        }

        void GetPlayerInput()
        {
            moveVector = Vector3.zero;

            // Check Mouse Wheel Input prior to Shift Key so we can apply multiplier on Shift for Scrolling
            mouseWheel = Input.GetAxis("Mouse ScrollWheel");

            float touchCount = Input.touchCount;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || touchCount > 0)
            {
                mouseWheel *= 10;

                if (Input.GetKeyDown(KeyCode.I))
                    CameraMode = CameraModes.Isometric;

                if (Input.GetKeyDown(KeyCode.F))
                    CameraMode = CameraModes.Follow;

                if (Input.GetKeyDown(KeyCode.S))
                    MovementSmoothing = !MovementSmoothing;


                // Check for right mouse button to change camera follow and elevation angle
                if (Input.GetMouseButton(1))
                {
                    mouseY = Input.GetAxis("Mouse Y");
                    mouseX = Input.GetAxis("Mouse X");

                    if (mouseY > 0.01f || mouseY < -0.01f)
                    {
                        ElevationAngle -= mouseY * MoveSensitivity;
                        // Limit Elevation angle between min & max values.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }

                    if (mouseX > 0.01f || mouseX < -0.01f)
                    {
                        OrbitalAngle += mouseX * MoveSensitivity;
                        if (OrbitalAngle > 360)
                            OrbitalAngle -= 360;
                        if (OrbitalAngle < 0)
                            OrbitalAngle += 360;
                    }
                }

                // Get Input from Mobile Device
                if (touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;

                    // Handle elevation changes
                    if (deltaPosition.y > 0.01f || deltaPosition.y < -0.01f)
                    {
                        ElevationAngle -= deltaPosition.y * 0.1f;
                        // Limit Elevation angle between min & max values.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }


                    // Handle left & right 
                    if (deltaPosition.x > 0.01f || deltaPosition.x < -0.01f)
                    {
                        OrbitalAngle += deltaPosition.x * 0.1f;
                        if (OrbitalAngle > 360)
                            OrbitalAngle -= 360;
                        if (OrbitalAngle < 0)
                            OrbitalAngle += 360;
                    }

                }

                // Check for left mouse button to select a new CameraTarget or to reset Follow position
                if (Input.GetMouseButton(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 300, 1 << 10 | 1 << 11 | 1 << 12 | 1 << 14))
                    {
                        if (hit.transform == CameraTarget)
                        {
                            // Reset Follow Position
                            OrbitalAngle = 0;
                        }
                        else
                        {
                            CameraTarget = hit.transform;
                            OrbitalAngle = 0;
                            MovementSmoothing = previousSmoothing;
                        }

                    }
                }


                if (Input.GetMouseButton(2))
                {
                    if (dummyTarget == null)
                    {
                        // We need a Dummy Target to anchor the Camera
                        dummyTarget = new GameObject("Camera Target").transform;
                        dummyTarget.position = CameraTarget.position;
                        dummyTarget.rotation = CameraTarget.rotation;
                        CameraTarget = dummyTarget;
                        previousSmoothing = MovementSmoothing;
                        MovementSmoothing = false;
                    }
                    else if (dummyTarget != CameraTarget)
                    {
                        // Move DummyTarget to CameraTarget
                        dummyTarget.position = CameraTarget.position;
                        dummyTarget.rotation = CameraTarget.rotation;
                        CameraTarget = dummyTarget;
                        previousSmoothing = MovementSmoothing;
                        MovementSmoothing = false;
                    }


                    mouseY = Input.GetAxis("Mouse Y");
                    mouseX = Input.GetAxis("Mouse X");

                    moveVector = cameraTransform.TransformDirection(mouseX, mouseY, 0);

                    dummyTarget.Translate(-moveVector, Space.World);

                }

            }

            // Check Pinching to Zoom in - out on Mobile device
            if (touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                float prevTouchDelta = (touch0PrevPos - touch1PrevPos).magnitude;
                float touchDelta = (touch0.position - touch1.position).magnitude;

                float zoomDelta = prevTouchDelta - touchDelta;

                if (zoomDelta > 0.01f || zoomDelta < -0.01f)
                {
                    FollowDistance += zoomDelta * 0.25f;
                    // Limit FollowDistance between min & max values.
                    FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
                }


            }

            // Check MouseWheel to Zoom in-out
            if (mouseWheel < -0.01f || mouseWheel > 0.01f)
            {

                FollowDistance -= mouseWheel * 5.0f;
                // Limit FollowDistance between min & max values.
                FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
            }


        }
    }
}

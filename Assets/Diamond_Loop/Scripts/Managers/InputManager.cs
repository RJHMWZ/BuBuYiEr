using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ClawbearGames
{
    /// <summary>
    /// InputManager - A static wrapper for Unity's new Input System (v1.19.0+)
    /// Provides easy-to-use methods similar to the old Input system
    /// Works with both mouse (PC) and touch (mobile) input
    /// </summary>
    public static class InputManager
    {
        private static bool wasPressed;
        private static bool isPressed;
        private static bool isInitialized = false;
        private static EventSystem raycastSystem;
        private static PointerEventData raycastPointer;
        private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>(24);

        private static void RaycastUI(Vector2 position)
        {
            raycastResults.Clear();
            EventSystem current = EventSystem.current;
            if (current == null) return;
            if (raycastPointer == null || raycastSystem != current)
            {
                raycastSystem = current;
                raycastPointer = new PointerEventData(current);
            }
            raycastPointer.Reset();
            raycastPointer.position = position;
            current.RaycastAll(raycastPointer, raycastResults);
        }

        /// <summary>
        /// Initialize the InputManager. Call this once at game startup (e.g., in Awake)
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
                return;

            // Enable enhanced touch support for better multi-touch handling
            EnhancedTouchSupport.Enable();
            isInitialized = true;
        }

        /// <summary>
        /// Update input state. Call this once per frame in Update()
        /// </summary>
        public static void Update()
        {
            if (!isInitialized)
                Initialize();

            wasPressed = isPressed;

            // Use Pointer.current for unified mouse/touch support
            if (Pointer.current != null)
            {
                isPressed = Pointer.current.press.isPressed;
            }
            else
            {
                isPressed = false;
            }
        }

        #region Single Input (Mouse/Primary Touch)

        /// <summary>
        /// Check if pointer was pressed this frame (equivalent to Input.GetMouseButtonDown(0))
        /// </summary>
        /// <param name="checkUI">If true, returns false when touching UI</param>
        public static bool GetMouseButtonDown(bool checkUI = false)
        {
            bool result = isPressed && !wasPressed;
            return checkUI ? result && !IsPointerOverUI() : result;
        }

        /// <summary>
        /// Check if pointer is currently pressed (equivalent to Input.GetMouseButton(0))
        /// </summary>
        /// <param name="checkUI">If true, returns false when touching UI</param>
        public static bool GetMouseButton(bool checkUI = false)
        {
            return checkUI ? isPressed && !IsPointerOverUI() : isPressed;
        }

        /// <summary>
        /// Check if pointer was released this frame (equivalent to Input.GetMouseButtonUp(0))
        /// </summary>
        /// <param name="checkUI">If true, returns false when touching UI</param>
        public static bool GetMouseButtonUp(bool checkUI = false)
        {
            bool result = !isPressed && wasPressed;
            return checkUI ? result && !IsPointerOverUI() : result;
        }

        /// <summary>
        /// Get current pointer position in screen space (works for both mouse and touch)
        /// </summary>
        public static Vector2 GetMousePosition()
        {
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
            return Vector2.zero;
        }

        /// <summary>
        /// Get world position using plane raycast
        /// </summary>
        /// <param name="plane">The plane to raycast against</param>
        /// <param name="camera">Camera to use (null = Camera.main)</param>
        public static Vector3 GetWorldPosition(Plane plane, Camera camera = null)
        {
            if (camera == null)
                camera = Camera.main;

            if (camera == null)
                return Vector3.zero;

            Vector2 screenPos = GetMousePosition();
            Ray ray = camera.ScreenPointToRay(screenPos);

            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Get world position on horizontal plane at specified Y height
        /// </summary>
        public static Vector3 GetWorldPosition(float yHeight = 0f, Camera camera = null)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0, yHeight, 0));
            return GetWorldPosition(plane, camera);
        }

        #endregion

        #region UI Detection

        /// <summary>
        /// Check if pointer is currently over any UI element
        /// Single touch only
        /// </summary>
        public static bool IsPointerOverUI()
        {
            RaycastUI(GetMousePosition());
            return raycastResults.Count > 0;
        }

        /// <summary>
        /// Get the UI object currently under the pointer (returns null if none)
        /// Single touch only
        /// </summary>
        public static GameObject GetCurrentUIObject()
        {
            RaycastUI(GetMousePosition());
            return raycastResults.Count > 0 ? raycastResults[0].gameObject : null;
        }

        /// <summary>
        /// Check if a specific UI GameObject is currently being touched/hovered
        /// Single touch only
        /// </summary>
        public static bool IsTouchingUIObject(GameObject uiObject)
        {
            if (uiObject == null) return false;
            RaycastUI(GetMousePosition());
            foreach (RaycastResult result in raycastResults)
                if (result.gameObject == uiObject) return true;
            return false;
        }

        #endregion

        #region Multi-Touch Support (Mobile Only)

        /// <summary>
        /// Get the number of active touches (mobile only, returns 0 on PC)
        /// (mobile, multi touch only)
        /// </summary>
        public static int GetTouchCount()
        {
            return Touch.activeTouches.Count;
        }

        /// <summary>
        /// Get all active touch positions (mobile, multi touch only)
        /// </summary>
        public static List<Vector2> GetAllTouchPositions()
        {
            List<Vector2> positions = new List<Vector2>();

            foreach (var touch in Touch.activeTouches)
            {
                positions.Add(touch.screenPosition);
            }

            return positions;
        }

        /// <summary>
        /// Get touch position by index (0 = first touch, 1 = second touch, etc.)
        /// Mobile only - returns Vector2.zero on PC or if index is invalid
        /// </summary>
        public static Vector2 GetTouchPosition(int index)
        {
            if (index < 0 || index >= Touch.activeTouches.Count)
                return Vector2.zero;

            return Touch.activeTouches[index].screenPosition;
        }

        /// <summary>
        /// Get touch phase by index (Began, Moved, Stationary, Ended, Canceled)
        /// </summary>
        public static UnityEngine.InputSystem.TouchPhase GetTouchPhase(int index)
        {
            if (index < 0 || index >= Touch.activeTouches.Count)
                return UnityEngine.InputSystem.TouchPhase.None;

            return Touch.activeTouches[index].phase;
        }

        /// <summary>
        /// Check if any touch is currently over UI (mobile, multi touch only)
        /// </summary>
        public static bool IsAnyTouchOverUI()
        {
            if (EventSystem.current == null)
                return false;

            List<Vector2> touchPositions = GetAllTouchPositions();

            foreach (Vector2 touchPos in touchPositions)
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current) { position = touchPos };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                if (results.Count > 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get all UI objects being touched by any finger (mobile, multi touch only)
        /// </summary>
        public static List<GameObject> GetAllTouchedUIObjects()
        {
            List<GameObject> touchedObjects = new List<GameObject>();

            if (EventSystem.current == null)
                return touchedObjects;

            List<Vector2> touchPositions = GetAllTouchPositions();

            foreach (Vector2 touchPos in touchPositions)
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current) { position = touchPos };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                if (results.Count > 0 && !touchedObjects.Contains(results[0].gameObject))
                {
                    touchedObjects.Add(results[0].gameObject);
                }
            }

            return touchedObjects;
        }

        /// <summary>
        /// Check if a specific touch index is over UI (mobile, multi touch only)
        /// </summary>
        public static bool IsTouchOverUI(int touchIndex)
        {
            if (EventSystem.current == null)
                return false;

            Vector2 touchPos = GetTouchPosition(touchIndex);
            if (touchPos == Vector2.zero)
                return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = touchPos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            return results.Count > 0;
        }

        /// <summary>
        /// Get world position for a specific touch using plane raycast (mobile, multi touch only)
        /// </summary>
        public static Vector3 GetTouchWorldPosition(int touchIndex, Plane plane, Camera camera = null)
        {
            if (camera == null)
                camera = Camera.main;

            if (camera == null)
                return Vector3.zero;

            Vector2 touchPos = GetTouchPosition(touchIndex);
            if (touchPos == Vector2.zero)
                return Vector3.zero;

            Ray ray = camera.ScreenPointToRay(touchPos);

            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Get world position for a specific touch on horizontal plane (mobile, multi touch only)
        /// </summary>
        public static Vector3 GetTouchWorldPosition(int touchIndex, float yHeight = 0f, Camera camera = null)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0, yHeight, 0));
            return GetTouchWorldPosition(touchIndex, plane, camera);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if the device has touch support
        /// </summary>
        public static bool IsTouchSupported()
        {
            return Touchscreen.current != null;
        }

        /// <summary>
        /// Get delta movement of the pointer (useful for camera dragging)
        /// </summary>
        public static Vector2 GetPointerDelta()
        {
            if (Pointer.current != null)
            {
                return Pointer.current.delta.ReadValue();
            }
            return Vector2.zero;
        }

        #endregion
    }
}
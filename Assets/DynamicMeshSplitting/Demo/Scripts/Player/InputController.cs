using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Demo
{
    public static class InputController
    {
        private const string _runXAxis = "RunX";
        private const string _runYAxis = "RunY";
        private const string _lookXAxis = "LookX";
        private const string _lookYAxis = "LookY";

        private const string _jumpButton = "Jump";
        private const string _restartButton = "Restart";

        public static float RunXAxis => Input.GetAxis(_runXAxis);
        public static float RunYAxis => Input.GetAxis(_runYAxis);

        public static float LookXAxis => Input.GetAxis(_lookXAxis);
        public static float LookYAxis => Input.GetAxis(_lookYAxis);

        public static bool JumpButtonDown => Input.GetButtonDown(_jumpButton);
        public static bool InteractButtonDown => Input.GetMouseButtonDown(1);
        public static bool InteractButtonUp => Input.GetMouseButtonUp(1);
        public static bool InteractButton => Input.GetMouseButton(1);
        public static bool SplitButtonDown => Input.GetMouseButtonDown(0);
        public static bool RestartButtonDown => Input.GetButtonDown(_restartButton);
        public static bool WireframeButtonDown => Input.GetKeyDown(KeyCode.T);
        public static bool MenuButtonDown => Input.GetKeyDown(KeyCode.Escape);
    }
}

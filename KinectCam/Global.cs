 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectCam
{
    class Global
    {
        public const float DISTANCE = 3.5F;
        public const int DIFFERENCE = 20;

        public const int BACK_FACING_TO_CAMERA = 0;
        public const int FACING_TO_CAMERA = 1;
        public const int NONE = -1;
        public const int ENTERING = 0;
        public const int LEAVING = 1;

        public const int CAMERA_WIDTH = 640;
        public const int CAMERA_HEIGHT = 480;

        public const int HAND_DIAMETER = 100; // 0.1m

        public const int METER_TO_MILIMETER = 1000;
    }
}

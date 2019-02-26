using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
namespace skeleton
{
    public class waveGesture
    {
        private const float WAVE_THRESHOLD = 0.2f;
        private const int REQUIRED_ITERATIONS = 4;
        private const int LEFT_HAND = 0;
        private const int RIGHT_HAND = 1;
        private WaveGestureTracker[] _PlayerWaveTracker = new WaveGestureTracker[6];
        private event EventHandler GestureDetected;
        private int code = 0;
        private int code2 = 0;

        private enum WavePosition
        {
            None = 0,
            up = 1,
            down = 2,
            Neutral = 3
        }
        private enum WaveGestureState
        {
            None = 0,
            Success = 1,
            Failure = 2,
            Inprogress = 3
        }
        private struct WaveGestureTracker
        {
            public int IterationCount;
            public WaveGestureState State;
            public WavePosition StartPosition;
            public WavePosition CurrentPosition;

            public void Reset()
            {
                IterationCount = 0;
                State = WaveGestureState.None;
                StartPosition = WavePosition.None;
                CurrentPosition = WavePosition.None;
            }

            internal void UpdatePosition(WavePosition position)
            {
                if(CurrentPosition != position)
                {
                    if (position == WavePosition.up)
                    {
                        if (State != WaveGestureState.Inprogress)
                        {
                            State = WaveGestureState.Inprogress;
                            IterationCount = 0;
                            StartPosition = position;
                        }
                        CurrentPosition = position;
                    }
                    else if (CurrentPosition == WavePosition.up && position == WavePosition.down)
                    {
                        if (State == WaveGestureState.Inprogress)
                        {
                            IterationCount++;
                        }
                        CurrentPosition = position;
                    }
                    else if (position == WavePosition.down)
                    {
                        IterationCount = 0;
                        UpdateState(WaveGestureState.Failure);
                        CurrentPosition = position;
                    }
                    
                }
               
            }

            internal void UpdateState(WaveGestureState state)
            {
                State = state;
            }
        }

       public int Update(Skeleton[] skeletons , double shangle)
        {
            if(skeletons != null)
            {
                Skeleton skeleton;

                for(int i = 0; i < skeletons.Length; i++)
                {
                    skeleton = skeletons[i];

                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            code2 = TrackWave(skeleton, ref this._PlayerWaveTracker[i], shangle);
                       
                        }
                        else
                        {
                            this._PlayerWaveTracker[i].Reset();
                        }
                }
            }
            return code2;
        }

        private int TrackWave(Skeleton skeleton, ref WaveGestureTracker tracker, double shangle)
        {
            Joint hand = skeleton.Joints[JointType.HandRight];
            Joint elbow = skeleton.Joints[JointType.ElbowRight];

            if(hand.TrackingState != JointTrackingState.NotTracked && elbow.TrackingState != JointTrackingState.NotTracked)
            {
                //여기수정
                if (shangle >= 70 && shangle <= 110)
                {
                    if (hand.Position.Z >= elbow.Position.Z - WAVE_THRESHOLD && hand.Position.Z <= elbow.Position.Z + WAVE_THRESHOLD)
                    {
                        if (hand.Position.X >= elbow.Position.X - WAVE_THRESHOLD && hand.Position.X <= elbow.Position.X + WAVE_THRESHOLD)
                        {
                            tracker.UpdatePosition(WavePosition.up);
                        }
                    }
                    else if (hand.Position.Y <= elbow.Position.Y && hand.Position.Z <= elbow.Position.Z - WAVE_THRESHOLD)
                    {
                        tracker.UpdatePosition(WavePosition.down);
                    }
                    if(tracker.IterationCount >= 2)
                    {
                        code = 1;
                    }
                    else
                    {
                        code = 0;
                    }

                }
                else
                {
                    if(tracker.State == WaveGestureState.Inprogress)
                    {
                        tracker.UpdateState(WaveGestureState.Failure);
                        tracker.UpdatePosition(WavePosition.None);
                    }
                    else
                    {
                        tracker.Reset();
                    }
                    code = 0;
                }
            }
            else
            {
                tracker.Reset();
                
            }
            
            return code;
        }
    }
}

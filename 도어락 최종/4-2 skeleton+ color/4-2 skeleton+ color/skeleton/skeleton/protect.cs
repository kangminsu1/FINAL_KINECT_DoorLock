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
using System.Windows.Threading;
// 카운터와 각도 계산


namespace skeleton
{
    class protect
    {
        DispatcherTimer timer = new DispatcherTimer();
        private int sw = 0;
        private int code = 0;
        private int pre = 0;
        private int count = 0;
        private int num = 0;

        public int Update(double right, double left, Point re, Point rs, Point le, Point ls)
        {
            if (re.Y <= rs.Y + 25 && le.Y <= ls.Y + 25)
            {

                if (right >= 70 && right <= 110)
                {
                    if (left >= 70 && right <= 110)
                    {
                        sw = 1;

                    }
                    else
                    {
                        sw = 0;
                    }
                }
                else
                {
                    sw = 0;
                }

            }
            else
            {
                sw = 0;
            }
            if (sw != pre && sw == 1)
            {
                pre = 1;
                count = 1;
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += tick;
                timer.Start();
            }
            else if (sw != pre && sw == 0)
            {
                pre = 0;
                count = 0;
                num = 0;
                timer.Stop();
                timer.Tick -= tick;
            }

            if (num >= 6 && sw == 1)
            {
                code = 1;
            }
            else
            {
                code = 0;
            }

            return code;
        }

        private void tick(object sender, EventArgs e)
        {
            num++;
        }



        /*       private void Timer_Tick(object sender, EventArgs e)
               {
                   time++;
               }*/
    }
}

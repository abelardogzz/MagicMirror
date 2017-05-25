//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

/// <summary>
/// Magic Mirror
/// Proyecto que se enfoca en rastrear las extremidades de la persona frente al kinect
/// Sobreponiendo imagenes de un personaje(espesificado) en las extremidades de la persona
/// Siguiendo los movimientos de las personas como si estuviera reflejado en un espejo
/// </summary>
namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System;
    using System.Windows.Controls;

    // using System.Drawing;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing 
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 6;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Maneja el cierre de la ventana
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.White, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                            ///Aqui a huevo         ///AQUI ES POSIBLE QUE SE TRABAJE
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            //this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter); //1
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter, 1);//1
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);//2
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);//3
//this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);//10
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine, 10);//10
 //this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);//11
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter, 11);//11 
            //this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);//12
            //this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);//13

            // Left Arm
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);//4
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft, 4);//4
//this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);//5
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft, 5);//5
//this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);//6
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft, 6);//6

            // Right Arm
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);//7
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight, 7);//7
//this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);//8
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight, 8);//8
//this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);//9
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight, 9);//9

            // Left Leg
            //this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);//14
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft, 14);//14
///this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);//15
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft, 15);//15
///this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);//16
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft, 16);//16

            // Right Leg
            //this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);//17
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight, 17);//17
///this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);//18
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight, 18);//18
///this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);//19
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight, 19);//19

            // Render Joints
            //Para propositos de este proyecto no se necesita dibujar las
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    //drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
                //drawPen = new Pen(Brushes.DeepSkyBlue, 5.0F);
            }

            


        }

        /// <summary>
        /// Dibuja las partes del cuerpo segun los Joints y ademas gira segun el angulo que se cree entre los Joints 
        /// Crean el efecto de movimiento en 2-dimensiones
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        /// <param name="parte">name that identifies which bone is drawn</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1, int parte)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            Point Ori = this.SkeletonPointToScreen(joint0.Position);
            Point Dest = this.SkeletonPointToScreen(joint1.Position);
            Vector v1 = new Vector(Ori.X, Ori.Y);
            Vector v2 = new Vector(Dest.X, Dest.Y);
            double widthy = Dest.X - Ori.X;
            double heighty = Dest.Y - Ori.Y;

            if (widthy >= 0 || heighty >= 0)
            {
                widthy = 50;
                heighty = 50;
            }

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
                
            }
            
            double angle;
            RotateTransform rotate;
            //Identifica cada parte del cuerpo siguiendo un orden logico segun los Joints detectados
            switch (parte)
            {//Cada caso es un hueso diferente
                case 1: //cabeza
                    //Obtiene el angulo entre los Joints espesificos
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform(); //Se crea para rotar la imagen
                    cabeza.Visibility = Visibility.Visible; //Se hace visible
                    //Fija la posicion de la cabeza en el joint correspondiente
                    Canvas.SetLeft(cabeza, this.SkeletonPointToScreen(joint0.Position).X + 25); 
                    Canvas.SetTop(cabeza, this.SkeletonPointToScreen(joint0.Position).Y + 10);
                    cabeza.RenderTransform = rotate;
                    rotate.Angle = angle; //Se rota la imagen
                    break;
                case 4: //brazoIzq
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    brazoIzq.Visibility = Visibility.Visible;
                    Canvas.SetLeft(brazoIzq, this.SkeletonPointToScreen(joint0.Position).X + 40);//Fija la posicion del brazo izquierdo en el joint correspondiente
                    Canvas.SetTop(brazoIzq, this.SkeletonPointToScreen(joint0.Position).Y + 10);
                    brazoIzq.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
                case 5: //antebrazoIzq
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    antebrazoIzq.Visibility = Visibility.Visible;
                    Canvas.SetLeft(antebrazoIzq, this.SkeletonPointToScreen(joint0.Position).X + 40); //Fija la posicion del antebrazo izquierdo en el joint correspondiente
                    Canvas.SetTop(antebrazoIzq, this.SkeletonPointToScreen(joint0.Position).Y + 10);
                    antebrazoIzq.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen

                    break;
                case 6: //manoIzq
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    manoIzq.Visibility = Visibility.Visible;
                    Canvas.SetLeft(manoIzq, this.SkeletonPointToScreen(joint0.Position).X + 35);//Fija la posicion de la mano izquierda en el joint correspondiente
                    Canvas.SetTop(manoIzq, this.SkeletonPointToScreen(joint0.Position).Y + 15);
                    manoIzq.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
                case 7: //brazoDer
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    brazoDer.Visibility = Visibility.Visible;
                    Canvas.SetLeft(brazoDer, this.SkeletonPointToScreen(joint0.Position).X + 50);//Fija la posicion del brazo derecho en el joint correspondiente
                    Canvas.SetTop(brazoDer, this.SkeletonPointToScreen(joint0.Position).Y + 10);
                    brazoDer.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
                case 8: //antebrazoDer
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    antebrazoDer.Visibility = Visibility.Visible;
                    Canvas.SetLeft(antebrazoDer, this.SkeletonPointToScreen(joint0.Position).X + 50);//Fija la posicion del antebrazo derecho en el joint correspondiente
                    Canvas.SetTop(antebrazoDer, this.SkeletonPointToScreen(joint0.Position).Y);
                    antebrazoDer.RenderTransform = rotate;
                    rotate.Angle = angle;
                    break;
                case 9: //manoDer
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    manoDer.Visibility = Visibility.Visible;
                    Canvas.SetLeft(manoDer, this.SkeletonPointToScreen(joint0.Position).X + 45);//Fija la posicion de la mano derecha en el joint correspondiente
                    Canvas.SetTop(manoDer, this.SkeletonPointToScreen(joint0.Position).Y);
                    manoDer.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;

                case 10: //Torso
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    torso.Visibility = Visibility.Visible;
                    Canvas.SetLeft(torso, this.SkeletonPointToScreen(joint0.Position).X + 20);//Fija la posicion del torso en el joint correspondiente
                    Canvas.SetTop(torso, this.SkeletonPointToScreen(joint0.Position).Y + 30);
                    torso.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
                case 11: //Cadera
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    cadera.Visibility = Visibility.Visible;
                    Canvas.SetLeft(cadera, this.SkeletonPointToScreen(joint0.Position).X + 20);//Fija la posicion de la cadera en el joint correspondiente
                    Canvas.SetTop(cadera, this.SkeletonPointToScreen(joint0.Position).Y + 20);
                    cadera.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;

                case 14://musloIzq
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    musloIzq.Visibility = Visibility.Visible;
                    Canvas.SetLeft(musloIzq, this.SkeletonPointToScreen(joint0.Position).X + 40);//Fija la posicion del muslo izquierdo en el joint correspondiente
                    Canvas.SetTop(musloIzq, this.SkeletonPointToScreen(joint0.Position).Y + 25);
                    musloIzq.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
                case 15://femurIzq
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    femurIzq.Visibility = Visibility.Visible;
                    Canvas.SetLeft(femurIzq, this.SkeletonPointToScreen(joint0.Position).X + 50);//Fija la posicion del femur izquierdo en el joint correspondiente
                    Canvas.SetTop(femurIzq, this.SkeletonPointToScreen(joint0.Position).Y);
                    femurIzq.RenderTransform = rotate;
                    rotate.Angle = angle;
                    break;
                case 16://pieIzq
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    pieIzq.Visibility = Visibility.Visible;
                    Canvas.SetLeft(pieIzq, this.SkeletonPointToScreen(joint0.Position).X + 30);//Fija la posicion de la pieIzq en el joint correspondiente
                    Canvas.SetTop(pieIzq, this.SkeletonPointToScreen(joint0.Position).Y );
                    pieIzq.RenderTransform = rotate;
                    rotate.Angle = angle;
                    break;
                case 17://musloDer
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    musloDer.Visibility = Visibility.Visible;
                    Canvas.SetLeft(musloDer, this.SkeletonPointToScreen(joint0.Position).X + 40);//Fija la posicion del muslo derecho en el joint correspondiente
                    Canvas.SetTop(musloDer, this.SkeletonPointToScreen(joint0.Position).Y + 25);
                    musloDer.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
                case 18://femurDer
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    femurDer.Visibility = Visibility.Visible;
                    Canvas.SetLeft(femurDer, this.SkeletonPointToScreen(joint0.Position).X + 40);//Fija la posicion del femur derecho en el joint correspondiente
                    Canvas.SetTop(femurDer, this.SkeletonPointToScreen(joint0.Position).Y);
                    femurDer.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
                case 19: //pieDer
                    angle = GetAngleFromPoint(this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                    rotate = new RotateTransform();
                    pieDer.Visibility = Visibility.Visible;
                    Canvas.SetLeft(pieDer, this.SkeletonPointToScreen(joint0.Position).X + 60);//Fija la posicion del pie derecho en el joint correspondiente
                    Canvas.SetTop(pieDer, this.SkeletonPointToScreen(joint0.Position).Y -10 );
                    pieDer.RenderTransform = rotate;
                    rotate.Angle = angle;  //Se rota la imagen
                    break;
            }
        }

        /// <summary>
        /// Obtiene el angulo entre dos Points
        /// </summary>
        /// <param name="point">El punto de origen</param>
        /// <param name="centerPoint">El punto de destino</param>
        /// <returns></returns>
        internal double GetAngleFromPoint(System.Windows.Point point, System.Windows.Point centerPoint) //
        {
            double dy = (point.Y - centerPoint.Y);
            double dx = (point.X - centerPoint.X);

            double theta = Math.Atan2(dy, dx);

            double angle = (-270 + ((theta * 180) / Math.PI)) % 360;

            return angle;
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
       /* private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }*/
    }
}
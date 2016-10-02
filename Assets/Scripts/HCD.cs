/*
Copyright 2016 Etienne Sanquer
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/

using UnityEngine;
using System.Collections;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Threading;
using UnityEngine.UI;
public class HCD : MonoBehaviour
{
    private CascadeClassifier _FacescascadeClassifier;
    private CascadeClassifier _EyescascadeClassifier;
    private Capture capture;

    private Point[] eyes; // unused unless noise is reduced
    private Point face;
    private float focal = 721f; // camera focal on my laptop ( poorly approximated )
    private float pxTOMM = 0.0032f;  // pixel to milimeter ( poorly approximated )
    private float sensorHeight = 1f;
    private Vector3 ViewerPositionFromScreen;

    private Rectangle[] faces;
    private Rectangle faceROI;
    private Rectangle faceRect;

    public Camera camera;

    public Slider YOffsetSlider;
    public float leftValue;
    public Slider GaussianBlurDistanceSlider;
    public Slider RightValueSlider;
    public Slider TopValueSlider;
    public Slider BottomValueSlider;

    private Vector3 cameraInitialPos;
    private Vector3 initialViewerPos;
    private Image<Gray, byte> grayFrame;
    bool initialised  ;
    private float XOffset, YOffset;

    private float FOV_X;

    public Transform _screenCenter;

    float w, h;
    private Thread faceDetectionThread;

    private bool treadRun;

    // Use this for initialization
    void Start()
    {
        capture = new Capture(); //create a camera captue
        _FacescascadeClassifier = new CascadeClassifier("C:/Emgu/emgucv-windows-universal 3.0.0.2157/opencv/data/haarcascades/haarcascade_frontalface_default.xml");
        _EyescascadeClassifier = new CascadeClassifier("C:/Emgu/emgucv-windows-universal 3.0.0.2157/opencv/data/haarcascades/haarcascade_eye.xml");
        eyes = new Point[2];
        camera.transform.position = _screenCenter.transform.position;
        cameraInitialPos = camera.transform.position;
        initialised = false;
        faceDetectionThread = new Thread(new ThreadStart(faceDetection));
        var orig = camera.projectionMatrix;
        w = 2 * camera.nearClipPlane / orig.m00;
        h = 2 * camera.nearClipPlane / orig.m11;

        Debug.Log("w : " + w);
        Debug.Log("h : " + h);
        Debug.Log("near : " + camera.nearClipPlane);
        faceDetectionThread.Start();

        using (var imageFrame = capture.QueryFrame().ToImage<Bgr, Byte>())
        {
            if (imageFrame != null)
            {
                imageFrame.Save("C:\\Users\\esanquer\\Pictures\\unityPhoto_mesure.jpg");
            }
        }
    }

    // Update is called once per frame
    private void faceDetection()
    {
        treadRun = true;
        while (treadRun)
        {
            if (capture != null)
            {
                using (var imageFrame = capture.QueryFrame().ToImage<Bgr, Byte>())
                {
                    if (imageFrame != null)
                    {
                       // imageFrame._SmoothGaussian(5); // reduce noise on face detection between frames
                        grayFrame = imageFrame.Convert<Gray, byte>();
                        // setting the ROI for tracking & perfs
                        if (faceROI != null && initialised)
                        {
                            faces = _FacescascadeClassifier.DetectMultiScale(grayFrame, 1.1d, 3, new Size(faceRect.Width * 8 / 10, faceRect.Height * 8 / 10), new Size(faceRect.Width * 12 / 10, faceRect.Width * 12 / 10)); //the actual face detection happens here
                            if (faces.Length == 1)
                            {
                                faceRect = faces[0];

                                faceROI = new Rectangle(faces[0].X - 80, faces[0].Y - 80, faces[0].Width + 160, faces[0].Height + 160);
                                imageFrame.ROI = (new Rectangle(faceROI.X, faceROI.Y, faceROI.Width, faceROI.Height));
                                face = new Point(faces[0].X + (faces[0].Width / 2), faces[0].Y + (faces[0].Height / 2));

                            }

                        }
                        // grayframe._SmoothGaussian(5);
                        else
                        {
                            faces = _FacescascadeClassifier.DetectMultiScale(grayFrame, 1.1d, 3, new Size(grayFrame.Rows / 5, grayFrame.Rows / 5), new Size(grayFrame.Rows * 2 / 3, grayFrame.Rows * 2 / 3)); //the actual face detection happens here

                            if (faces.Length == 1)
                            {
                                faceROI = new Rectangle(faces[0].X - 80, faces[0].Y - 80, faces[0].Width + 160, faces[0].Height + 160);
                                imageFrame.ROI = (new Rectangle(faceROI.X, faceROI.Y, faceROI.Width, faceROI.Height));
                                faceRect = faces[0];
                                face = new Point(faces[0].X + (faces[0].Width / 2), faces[0].Y + (faces[0].Height / 2));

                                // debug :
                                // grayframe.ROI = faceROI;
                                /* imageFrame.Draw(faces[0], new Bgr(System.Drawing.Color.Blue), 3);
                                 imageFrame.Draw(faceROI, new Bgr(System.Drawing.Color.Green), 3);
                                 imageFrame.Draw(new Rectangle(face.X-1, face.Y-1, 2,2), new Bgr(System.Drawing.Color.Green), 3);
                                 imageFrame.Save("C:\\Users\\esanquer\\Pictures\\unityPhoto_faces.jpg");*/
                            }
                        }
                        /*  var StoreEyes = _EyescascadeClassifier.DetectMultiScale(grayframe, 1.2d,10, Size.Empty, Size.Empty);
                          if (StoreEyes.Length == 2)
                          {
                              for (int i = 0; i < StoreEyes.Length; i++)
                              {
                                  imageFrame.Draw(StoreEyes[i], new Bgr(System.Drawing.Color.Blue), 2);
                                  eyes[i] = new Point((StoreEyes[i].X + StoreEyes[i].Width) / 2 + StoreEyes[i].X, (StoreEyes[i].Y + StoreEyes[i].Height) / 2 + StoreEyes[i].Y);
                              }
                          }*/

                    }
                }
            }
        }
    }

    void Update()
    {
        
        ViewerPositionFromScreen = caculateFacePosition();
        if (!initialised)
        {
            initialised = true;
            initialViewerPos = ViewerPositionFromScreen;
            XOffset = initialViewerPos.x;
            YOffset = initialViewerPos.x;
            Debug.Log(XOffset);
        }
       // Debug.Log(ViewerPositionFromScreen.x +"  ;  " + ViewerPositionFromScreen.y + "   ;   " + ViewerPositionFromScreen.z);

        Vector3 meterDeltaPos = ConvertFromMilimeterToUnity(ViewerPositionFromScreen );
        // Debug.Log(ViewerPositionFromScreen.x + "  ;  " + ViewerPositionFromScreen.y + "   ;   " + ViewerPositionFromScreen.z);
        // Debug.Log("to unity unit : ");
        //  Debug.Log(meterDeltaPos.x + "  ;  " + meterDeltaPos.y + "   ;   " + meterDeltaPos.z);
        Vector3 pos = _screenCenter.position - meterDeltaPos;
        camera.transform.position = pos;
        camera.transform.LookAt(_screenCenter.position + meterDeltaPos);
        /*   float left = LeftValueSlider.value;
           float right = RightValueSlider.value;
           float top = TopValueSlider.value;
           float bottom = BottomValueSlider.value;

           var m = cam.projectionMatrix;
           var w = 2 * cam.nearClipPlane / m.m00;
           var h = 2 * cam.nearClipPlane / m.m11;
           */



        var left = -w / 2 + meterDeltaPos.x*0.1f ;
        var right = left + w ;
        var bottom = -h / 2 + meterDeltaPos.y*0.1f;
        var top = bottom + h;

        //Debug.Log(meterDeltaPos.z);
        var near = camera.nearClipPlane + meterDeltaPos.z*0.1f;
        Debug.Log(near);
       // camera.fieldOfView = CalculateFOV();
        Matrix4x4 m = PerspectiveOffCenter(left,right, bottom, top, near, camera.farClipPlane);
        camera.projectionMatrix = m;
        
        //CalculateFOV();
    }

    public void OnDestroy()
    {
        treadRun = false;
        capture.Dispose();
        capture = null;

    }

    private Vector3 ConvertFromMilimeterToUnity(Vector3 p)
    {
        return new Vector3(p.x/1000 , p.y/1000 , p.z/1000 ); //mm to m
    }

    private Vector3 calculateViewerPositionFromScreen()
    {
        Point eye1 = eyes[0];
        Point eye2 = eyes[1];

        int xPX = (eye2.X + eye1.X) / 2;
        int yPX = (eye2.Y + eye1.Y) / 2;

        float distancePX = Mathf.Sqrt(Mathf.Pow(eye2.Y - eye1.Y, 2) + Mathf.Pow(eye2.X - eye1.X, 2));
        float Pmm = 60f;

        float YposMM = (yPX * Pmm) / distancePX;
        float XposMM = (xPX * Pmm) / distancePX;


        float Zx = (focal * XposMM - (xPX * pxTOMM)) / (xPX * pxTOMM);
        float Zy = (focal * YposMM - (yPX * pxTOMM)) / (yPX * pxTOMM);
        float Z = (Zx + Zy) / 2;

        return new Vector3(XposMM, YposMM, Z);  

    }

    private Vector3 caculateFacePosition()
    {
        int xPX = face.X-320; //- (int)XOffset // recadrage en prenant comme centre du repère le centre de l'image pour avoir une position relative au centre de l'image capturée
        int yPX = face.Y-240;

        float distancePX = faceROI.Width; // on prend comme repere d'echelle la largeur du visage dans l'image
        float Pmm = 150f;  // on dit que  cette largeur vaut 150mm

        float YposMM =( (yPX * Pmm) / distancePX) + YOffsetSlider.value;
        float XposMM = (xPX * Pmm) / distancePX;

        float zmm = (focal * Pmm) / faceRect.Width;

        float px_to_mm = 150 / faceRect.Width;

       Debug.Log("x : " + XposMM + " ; y : " + YposMM + " z : " + zmm);
        
        return new Vector3(XposMM, YposMM , zmm);


    }

    private Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left); // focal x
        float y = 2.0F * near / (top - bottom); // focal y
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }


    private float CalculateFOV()
    {
        Vector2 posPlanYZ = new Vector2(ViewerPositionFromScreen.y, ViewerPositionFromScreen.z);

        Vector2 screenBorderTop = new Vector2(posPlanYZ.x - (194.31f / 2), 0);  //345.44f = hauteur de l'écran laptop en mm
        Vector2 screenBorderBottom= new Vector2(posPlanYZ.x + (194.31f / 2), 0);
        float angle = Vector2.Angle(screenBorderTop - posPlanYZ, screenBorderBottom - posPlanYZ);
       // Debug.Log("Angle " + angle);
        return angle;
        

    }

    public void updateFocalFromSliderValue(float value)
    {
       // this.focal = value;
    }


    public void updatePxToMMFromSliderValue(float value)
    {
       // this.pxTOMM = value;
    }



    public void updateSensorHeightFromSliderValue(float value)
    {
        this.sensorHeight = value;
        pxTOMM = this.sensorHeight / 480;
    }


}

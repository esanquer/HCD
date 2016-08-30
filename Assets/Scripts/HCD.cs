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

public class HCD : MonoBehaviour
{
    private CascadeClassifier _FacescascadeClassifier;
    private CascadeClassifier _EyescascadeClassifier;
    private Capture capture;

    private Point[] eyes;
    private Point face;
    private float focal = 1.84f;
    private float pxTOMM = 0.0032f;

    private Vector3 ViewerPositionFromScreen;

    private Rectangle faceROI;

    public Camera camera;

    private Vector3 cameraInitialPos;
    private Vector3 initialViewerPos;
    private Image<Gray, byte> grayFrame;
    bool initialised  ;
    private float XOffset, YOffset;

    private float FOV_X;

    public Transform _screenCenter;

    // Use this for initialization
    void Start()
    {
       capture = new Capture(); //create a camera captue
        _FacescascadeClassifier = new CascadeClassifier("C:/Emgu/emgucv-windows-universal 3.0.0.2157/opencv/data/haarcascades/haarcascade_frontalface_default.xml");
        _EyescascadeClassifier = new CascadeClassifier("C:/Emgu/emgucv-windows-universal 3.0.0.2157/opencv/data/haarcascades/haarcascade_eye.xml");
        eyes = new Point[2];
        cameraInitialPos = camera.transform.position;
        initialised = false;
    }

    // Update is called once per frame
    void Update()
    {
        using (var imageFrame = capture.QueryFrame().ToImage<Bgr, Byte>())
        {
           
            if (imageFrame != null)
            {
                imageFrame._SmoothGaussian(29);
                grayFrame = imageFrame.Convert<Gray, byte>();
                if (faceROI != null && initialised)
                {
                   imageFrame.ROI = (new Rectangle(faceROI.X, faceROI.Y, faceROI.Width, faceROI.Height));
                }                
               // grayframe._SmoothGaussian(5);

                var faces = _FacescascadeClassifier.DetectMultiScale(grayFrame, 1.1d, 5, Size.Empty, Size.Empty); //the actual face detection happens here
                
                if (faces.Length == 1)
                {
                    faceROI = new Rectangle( faces[0].X - 50, faces[0].Y-50, faces[0].Width+100, faces[0].Height+100);

                    face = new Point(faces[0].X + (faces[0].Width/ 2),faces[0].Y +( faces[0].Height / 2));
                    
                    // grayframe.ROI = faceROI;
                   /* imageFrame.Draw(faces[0], new Bgr(System.Drawing.Color.Blue), 3);
                    imageFrame.Draw(faceROI, new Bgr(System.Drawing.Color.Green), 3);
                    imageFrame.Draw(new Rectangle(face.X-1, face.Y-1, 2,2), new Bgr(System.Drawing.Color.Green), 3);
                    imageFrame.Save("C:\\Users\\esanquer\\Pictures\\unityPhoto_faces.jpg");*/
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
        Debug.Log(ViewerPositionFromScreen.x + "  ;  " + ViewerPositionFromScreen.y + "   ;   " + ViewerPositionFromScreen.z);
       // Debug.Log("to unity unit : ");
      //  Debug.Log(meterDeltaPos.x + "  ;  " + meterDeltaPos.y + "   ;   " + meterDeltaPos.z);
        camera.transform.position = _screenCenter.position - meterDeltaPos;
        camera.transform.LookAt(_screenCenter.position);
        CalculateFOV();
    }

    public void OnDestroy()
    {
        capture.Dispose();
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

        float YposMM = (yPX * Pmm) / distancePX;
        float XposMM = (xPX * Pmm) / distancePX;


        float Zx = (focal * XposMM - (xPX * pxTOMM)) / (xPX * pxTOMM);
        float Zy = (focal * YposMM - (yPX * pxTOMM)) / (yPX * pxTOMM);
        float Z = (Zx + Zy) / 2;  // on moyenne deux mesures de distance en fonction de y et de y

        return new Vector3(XposMM, YposMM , Z);

    }

    private void CalculateFOV()
    {
        Vector2 posPlanXZ = new Vector2(ViewerPositionFromScreen.x, ViewerPositionFromScreen.z);

        Vector2 screenBorderLeft = new Vector2(posPlanXZ.x - (345.44f / 2), 0);  //345.44f = largeur de l'écran en mm
        Vector2 screenBorderRight = new Vector2(posPlanXZ.x + (345.44f / 2), 0);
        float angle = Vector2.Angle(screenBorderLeft - posPlanXZ, screenBorderRight - posPlanXZ);
       // Debug.Log("Angle " + angle);
        camera.fieldOfView = angle;
        

    }
}

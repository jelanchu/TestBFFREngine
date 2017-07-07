using Betaface;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine_IY_RealtimeFR_BF
{
    class BFFREngine
    {
        private static string className = typeof(BFFREngine).Name;
        private static readonly ILog logger = LogManager.GetLogger(typeof(BFFREngine));

        private int id = 0;
        public BetafaceFDRE fdre = null;
        private BetafaceDetectionSettings dSettings;

        private System.Object lockThis = new System.Object();

        // parameters for face detection engine
        private static int iFlags = BetafaceConsts.BETAFACE_DETECTFACES_BASIC_PROFILE;  //Patrick: What is this?

        // recognition related
        private static int numOfCandidate = 5;//RuleProperties.NumberOfCandidate;
        private List<string> blacklistKeys = new List<string>();
        private static double default_false_alarm = 0.001;
        private int[] match_indexes;
        private double[] match_scores;
        private int[] is_match;
        private List<byte[]> blacklistKeysinMemory = new List<byte[]>();

        public BFFREngine(int id, Boolean isCore)
        {
            this.id = id;
            initSDK(isCore);
        }

        private void initSDK(Boolean isCore)
        {
            logger.Info("Init FR engine id = " + id);
            //ci = new CultureInfo("en-US");
            // calculate processing time
            Stopwatch watch = new Stopwatch();
            watch.Start();

            long aRam = Process.GetCurrentProcess().PrivateMemorySize64;

            fdre = null;
            //Tool.logDebug("Loading Analytic Engine");
            try
            {
                fdre = new BetafaceFDRE();
            }
            catch (Exception n)
            {
                logger.Error("Failed to load Analytic Engine, message = " + n.Message);
                Environment.FailFast("Failed to load Analytic Engine, message = " + n.Message, n);
                return;
            };

            if (null == fdre)
            {
                logger.Error("Failed to load Analytic Engine. Analytic Engine object is NULL, exiting!");
                Environment.Exit(-1);
                return;
            };

            try
            {
                if (fdre.Init())
                {
                    logger.Info("Analytic Engine initialized");
                }
                else
                {
                    //MessageBoxCloser.CloseAll();
                    logger.Error("Fail to initialize Analytic Engine. No license file exist or wrong license file, exiting!");
                    Environment.Exit(-1);
                    return;
                };
            }
            catch (Exception e)
            {
                //MessageBoxCloser.CloseAll();
                logger.Error("Fail to initialize Analytic Engine. No license file exists or wrong license file, exiting!" + e.Message);
                Environment.Exit(-1);
                return;
            }

            long bRam = Process.GetCurrentProcess().PrivateMemorySize64;
            logger.Info("FR engine RAM: " + (bRam - aRam));
            //WrapperAgent.LogInfo(className, "FR engine RAM: " + (bRam - aRam));

            // engine parameters
            dSettings.flags = iFlags;
            dSettings.iMaxImageWidthPix = 640;//BFFRProperties.MaxImageWidthPix;
            dSettings.iMaxImageHeightPix = 480;//BFFRProperties.MaxImageHeightPix;
            dSettings.dMinFaceSizeOnImage = 0.05;//BFFRProperties.MinFaceSizeOnImage;
            dSettings.iMinFaceSizePix = 20;//BFFRProperties.MinFaceSizePix;
            dSettings.dAngleDegrees = 0.0;//BFFRProperties.AngleDegrees;
            dSettings.dAngleToleranceDegrees = 30.0;//BFFRProperties.AngleToleranceDegrees;
            dSettings.dMinDetectionScore = 0.6;//BFFRProperties.MinDetectionScore;
#if false
            if (isCore)
            {
                lock (lockThis)
                {
                    // init in-memory suspect database
                    blacklistKeysinMemory.Clear();
                }
                logger.Info("Load keys into engine [ " + id + " ] memory.");
                DatabaseHelper.getInstance().addUpdateHandler(id, updateKeys);
            }

            match_indexes = new int[numOfCandidate];
            match_scores = new double[numOfCandidate];
            is_match = new int[numOfCandidate];
#endif
            // calculate init engine time
            watch.Stop();
            logger.Info("Init engine time = [ " + watch.Elapsed.TotalSeconds + "sec ]");
            //WrapperAgent.LogInfo(className, "Init engine time = [ " + watch.Elapsed.TotalSeconds + "sec ]");
        }

        //public void processImage(string inputFilename) //FREngineResult processImage(string inputFilename)
        public void processImage(object file)
        {
            //FREngineResult faceResult = new FREngineResult();
            string inputFilename = (string) file;
            logger.Debug("Processing source image [ " + inputFilename + " ]");
            Collection<Tuple<byte[], double, string, double, string, string>> faces = detection(ref fdre, inputFilename); // do the detection
#if false

            faceResult.detectionScore = -1;
            faceResult.suspectID = "";
            faceResult.recognitionScore = -1;
            faceResult.candicatesJson = "";
            faceResult.keyBytes = null;
            if (faces.Count != 0)
            {
                faceResult.keyBytes = faces[0].Item1;
                //Int64 faceImage = faces[0].Item2;
                faceResult.detectionScore = faces[0].Item2;
                faceResult.suspectID = faces[0].Item3;
                faceResult.recognitionScore = faces[0].Item4;
                //string suspectKey = faces[0].Item6;
                //string suspectName      = faces[0].Item7;
                faceResult.candicatesJson = faces[0].Item6;
                char seperator = Path.DirectorySeparatorChar;

                //fdre.Betaface_ReleaseImage(fdre.GetState(), ref faceImage);
                logger.Info("Image: [ " + inputFilename + " ], Detection Score: [ " + faceResult.detectionScore + " ], Suspect ID: [ " + faceResult.suspectID + " ], Recognition Score: [ " + faceResult.recognitionScore + " ]");
            }

            return faceResult;
#endif
        }

        public void Dispose() {
            fdre.Dispose();
        }

        private Collection<Tuple<byte[], double, string, double, string, string>> detection(ref BetafaceFDRE fdre, string image_filename)
        {
            // calculate processing time
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Collection<Tuple<byte[], double, string, double, string, string>> faces = new Collection<Tuple<byte[], double, string, double, string, string>>();

            // 1. LOAD SOURCE IMAGE
            int returnValue = 0;
            Int64 srcImage = 0;
            returnValue = fdre.Betaface_LoadImage(fdre.GetState(), image_filename, out srcImage);
            //Tool.logDebug("[detection] : load image = [ " + image_filename + " ]");
            if (returnValue == BetafaceConsts.BETAFACE_OK)
            {

                // 2. DETECT FACE
                Int64 detectionResult = 0;
                int facesCount = 0;

                Stopwatch bfDetectFacesWatch = new Stopwatch();
                bfDetectFacesWatch.Start();

                returnValue = fdre.Betaface_DetectFaces(fdre.GetState(), srcImage, dSettings, out facesCount, out detectionResult);

                bfDetectFacesWatch.Stop();
                logger.Info("Image: [ " + image_filename + " ], Face detection time: [ " + bfDetectFacesWatch.Elapsed.TotalSeconds + " sec ]");

                if (returnValue == BetafaceConsts.BETAFACE_OK)
                {
                    // 3. HANDLE RESULT FACE BY FACE
                    for (int i = 0; i < facesCount; i++)
                    {
                        Tuple<byte[], double, string, double, string, string> face = parseResult(ref fdre, srcImage, detectionResult, 0, image_filename);
                        faces.Add(face);
                    }

                    // 2. RELEASE RESOURCE
                    fdre.Betaface_ReleaseDetectionResult(fdre.GetState(), ref detectionResult);//releas detection result
                }
                else
                {
                    logger.Warn("Image: [ " + image_filename + " ], DetectFaces failed, message = " + returnValue);
                }

                // 1. RELEASE RESOURCE
                fdre.Betaface_ReleaseImage(fdre.GetState(), ref srcImage); //releas srcImage (1)
            }
            else
            {
                logger.Warn("Image: [ " + image_filename + " ], LoadImage failed, message = " + returnValue);
                return null;
            }

            // calculate processing time
            watch.Stop();
            logger.Info("Image: [ " + image_filename + " ], Detection and recognition time: [ " + watch.Elapsed.TotalSeconds + " sec ]");

            //Tool.logDebug("[detection] : face count [ " + faces.Count + " ]");
            return faces;
        }

        private Tuple<byte[], double, string, double, string, string> parseResult(ref BetafaceFDRE fdre, Int64 srcImage, Int64 detectionResult, int index, string image_filename)
        {
            // 1. Get face info
            Int64 faceInfo = 0;
            //Tool.logDebug("[parseResult]");
            int returnValue = fdre.Betaface_GetFaceInfo(fdre.GetState(), detectionResult, index, out faceInfo);
            if (returnValue == BetafaceConsts.BETAFACE_OK)
            {

                // 2. Get face score
                double detectionScore = -1;
                returnValue = fdre.Betaface_GetFaceInfoDoubleParam(fdre.GetState(), faceInfo, BetafaceConsts.BETAFACE_FEATURE_FACE | BetafaceConsts.BETAFACE_PARAM_SCORE, out detectionScore);
                if (returnValue == BetafaceConsts.BETAFACE_OK)
                {
                    //TODO do something?
                }
                else
                {
                    logger.Error("Image: [ " + image_filename + " ], Get face score failed, message = " + returnValue);
                }

                // 3. Get face keyStopwatch watch = new Stopwatch();
                Stopwatch keyStopwatch = new Stopwatch();
                keyStopwatch.Start();

                int KeyLen = 0;
                IntPtr faceKey = IntPtr.Zero;
                byte[] faceKeyBytes = null;
                returnValue = fdre.Betaface_GenerateFaceKey(fdre.GetState(), srcImage, faceInfo, BetafaceConsts.BETAFACE_RECKEY_DEFAULT, out faceKey, out KeyLen);
                if (returnValue == BetafaceConsts.BETAFACE_OK)
                {
                    faceKeyBytes = new byte[KeyLen];
                    Marshal.Copy(faceKey, faceKeyBytes, 0, KeyLen);
                    fdre.Betaface_ReleaseFaceKey(fdre.GetState(), ref faceKey); // release key
                }
                else
                {
                    logger.Error("Image: [ " + image_filename + " ], Get face key failed, message = " + returnValue);
                }
                //Tool.logDebug("[Betaface_GenerateFaceKey]");
                keyStopwatch.Stop();
                logger.Info("Image: [ " + image_filename + " ], Key index generated time: [ " + keyStopwatch.Elapsed.TotalSeconds + " sec ]");

                // 4. Get crop face image
                //Int64 faceImage = 0;
                //Int64 croppedfaceinfo = 0;
                //returnValue = fdre.Betaface_CropFaceImage(fdre.GetState(), srcImage, faceInfo, ruleProps.distanceBetweenEyes, ruleProps.distanceFromTopToEyeline, ruleProps.deRotate, ruleProps.outputFaceWidth, ruleProps.outputFaceHeight, ruleProps.noUpScale, ruleProps.blackBackgroundColor, out faceImage, out croppedfaceinfo);
                //if (returnValue == BetafaceConsts.BETAFACE_OK)
                //{
                //    fdre.Betaface_ReleaseFaceInfo(fdre.GetState(), ref croppedfaceinfo); // release cropped face info
                //}
                //else
                //{
                //    Tool.logError("Get crop face failed, message = " + returnValue);
                //}

                // 5. Do recognition
                Tuple<string, double, string, string> suspect = null;
#if false
                lock (lockThis)
                {
                    if (blacklistKeys.Count > 0) suspect = recognition(ref fdre, faceKeyBytes);
                }
#endif
                // 6. Prepare result
                //Tuple<byte[], Int64, double, string, double, string, string> face = new Tuple<byte[], Int64, double, string, double, string, string>(faceKeyBytes, faceImage, detectionScore, suspect.Item1, suspect.Item2, suspect.Item3, suspect.Item4);
                Tuple<byte[], double, string, double, string, string> face = null;
                if (suspect != null) face = new Tuple<byte[], double, string, double, string, string>(faceKeyBytes, detectionScore, suspect.Item1, suspect.Item2, suspect.Item3, suspect.Item4);
                else face = new Tuple<byte[], double, string, double, string, string>(faceKeyBytes, detectionScore, "", -1, null, "");
                fdre.Betaface_ReleaseFaceInfo(fdre.GetState(), ref faceInfo); // release face info
                return face;
            }
            else
            {
                logger.Warn("Image: [ " + image_filename + " ], Get face info failed, message = " + returnValue);
                return null;
            }
        }
#if false
        private Tuple<string, double, string, string> recognition(ref BetafaceFDRE fdre, byte[] candidateKey)
        {
            // calculate processing time
            //Stopwatch watch = new Stopwatch();
            //watch.Start();
            // load in-memory suspect database
            long state = 0;

            fdre.Betaface_SearchIndex_Init(blacklistKeys.Count, ref state, blacklistKeysinMemory[0]);
            //Tool.logDebug("[recognition] blacklistKeys.Count = " + blacklistKeys.Count);
            for (int index = 0; index < blacklistKeys.Count; index++)
            {
                //Tool.logDebug("[recognition] index = " + index);
                fdre.Betaface_SearchIndex_SetKey(state, index, blacklistKeysinMemory[index]);
            }

            // do recognition
            int nmatches = Math.Min(numOfCandidate, blacklistKeys.Count);

            //Tool.logDebug("[recognition] nmatches = " + nmatches);

            fdre.Betaface_SearchIndex_Search(state, candidateKey, 0, blacklistKeys.Count, nmatches, 0, default_false_alarm, match_indexes, match_scores, is_match);
            string suspectID = "";
            //string suspectName = "n/a";
            double suspectScore = -1;
            string suspectKey = "n/a";
            string filename = Path.GetFileNameWithoutExtension(blacklistKeys[match_indexes[0]]);
            string[] tokens = filename.Split('-');

            if (match_scores[0] > RuleProperties.ThresholdScore)
            {
                suspectID = tokens[0];
                suspectScore = match_scores[0];
                suspectKey = blacklistKeys[match_indexes[0]];
                //Tool.logDebug("Recognition : suspect found *******************************");
                //Tool.logDebug("Recognition : suspect found *****[ " + suspectID + " ]*****");
                //Tool.logDebug("Recognition : suspect found *******************************");
            }

            //Tool.logDebug("[recognition] -- 2");
            //Tool.logDebug("Recognition : candicate suspect found *******************************");
            // TODO, new a candicate json array
            JObject jobj = new JObject();
            for (int m = 0; m < nmatches; m++)
            {
                string candicateFilename = Path.GetFileNameWithoutExtension(blacklistKeys[match_indexes[m]]);
                string[] candicateTokens = candicateFilename.Split('-');

                string candicateSuspectID = candicateTokens[0];
                double candicateSuspectScore = match_scores[m];

                //Tool.logDebug("Recognition : candicate suspect found *****[ " + candicateSuspectID + ", score: " + candicateSuspectScore + " ]*****");

                // TODO, add candidate json object
                JProperty jprop = new JProperty(candicateSuspectID, candicateSuspectScore);
                jobj.Add(jprop);
            }
            // TODO, translate candidate json array to string
            string candidates = JsonConvert.SerializeObject(jobj);

            //Tool.logDebug("[recognition] -- 3");
            //Tool.logDebug("Recognition : candicate suspect found *******************************");

            // release recognition engine
            fdre.Betaface_SearchIndex_Deinit(ref state);

            // calculate processing time
            //watch.Stop();
            //Tool.logDebug("[recognition] : suspectID [ " + suspectID + " ], suspectImg [ " + tokens[1] + " ], suspectScore [ " + match_scores[0] + "/" + ruleProps.recognitionScoreThreshold + " ], suspectKey [ " + blacklistKeys[match_indexes[0]] + " ], processing time = [ " + watch.Elapsed.TotalSeconds + "sec ], size [ " + blacklistKeys.Length + " ]");
            //Tool.logDebug("[recognition] -- 4");

            return Tuple.Create(suspectID, suspectScore, suspectKey, candidates);
        }

        private bool reloadBlackListKeys(List<string> keyFilenames)
        {
            lock (lockThis)
            {
                //Tool.logDebug("FR : Reloading BlackListKeys to Memory");
                blacklistKeys = keyFilenames;
                // init in-memory suspect database
                blacklistKeysinMemory.Clear();
                for (int index = 0; index < blacklistKeys.Count; index++)
                {
                    try
                    {
                        blacklistKeysinMemory.Add(File.ReadAllBytes(blacklistKeys[index]));
                    }
                    catch (Exception e)
                    {
                        logger.Warn(e.Message, e);
                    }
                }
            }
            logger.Info("Load Keys count [ " + blacklistKeys.Count + " ] into engine [ " + id + " ]");
            return true;
        }

        public Boolean compareKey(byte[] faceKey, List<string> keyFilenames)
        {
            Boolean result = false;

            // calculate processing time
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // reload 
            reloadBlackListKeys(keyFilenames);

            // load in-memory suspect database
            long state = 0;
            fdre.Betaface_SearchIndex_Init(blacklistKeys.Count, ref state, blacklistKeysinMemory[0]);
            for (int index = 0; index < blacklistKeys.Count; index++)
            {
                fdre.Betaface_SearchIndex_SetKey(state, index, blacklistKeysinMemory[index]);
            }

            // do recognition
            int nmatches = Math.Min(numOfCandidate, blacklistKeys.Count);

            fdre.Betaface_SearchIndex_Search(state, faceKey, 0, blacklistKeys.Count, nmatches, 0, default_false_alarm, match_indexes, match_scores, is_match);

            if (match_scores[0] > RuleProperties.ThresholdScore)
            {
                logger.Debug("[compareKeys] : matched keyfile = [ " + blacklistKeys[match_indexes[0]] + " ]");
                result = true;
            }

            // release recognition engine
            fdre.Betaface_SearchIndex_Deinit(ref state);

            // calculate processing time
            watch.Stop();
            logger.Debug("Processing time = [ " + watch.Elapsed.TotalSeconds + "sec ], size [ " + blacklistKeys.Count + " ]");

            return result;
        }

        public void updateKeys(object sender, EventHandlerArgs e)
        {
            List<string> newkeys = e.Parameter as List<string>;
            Boolean updateYes = false;

            if (newkeys.Count != 0)
            {
                if (blacklistKeys.Count != newkeys.Count)
                {
                    updateYes = true;
                }
                else
                {
                    var areEquivalent = !blacklistKeys.Except(newkeys).Any();
                    if (!areEquivalent)
                    {
                        updateYes = true;
                    }
                }
                if (updateYes == true)
                {
                    reloadBlackListKeys(newkeys);
                }

            }
        }
#endif
    }




    //public static class MessageBoxCloser
    //{
    //    public static void CloseAll()
    //    {
    //        // Close all message boxes displayed by any thread in the current process
    //        foreach (ProcessThread pt in Process.GetCurrentProcess().Threads)
    //        {
    //            EnumThreadWindows(pt.Id, new EnumThreadDelegate(closeDialog), IntPtr.Zero);
    //        }
    //    }
    //    public static int GetThreadId()
    //    {
    //        // Returns Thread ID of currently running thread.  Use it for the CloseThreadBoxes() method
    //        return GetCurrentThreadId();
    //    }
    //    public static bool CloseThreadBoxes(int threadId)
    //    {
    //        // Closes message box for thread <threadId>
    //        return EnumThreadWindows(threadId, new EnumThreadDelegate(closeDialog), IntPtr.Zero);
    //    }
    //    private static bool closeDialog(IntPtr hWnd, IntPtr lp)
    //    {
    //        // Check if it's a Windows dialog
    //        StringBuilder buf = new StringBuilder(256);
    //        GetClassName(hWnd, buf, buf.Capacity);
    //        Console.WriteLine(buf.ToString());
    //        if (buf.ToString() != "#32770") return false;
    //        // Got it, close it with WM_CLOSE
    //        SendMessage(hWnd, 0x10, IntPtr.Zero, IntPtr.Zero);
    //        return true;
    //    }
    //    // P/Invoke declarations
    //    private delegate bool EnumThreadDelegate(IntPtr hwnd, IntPtr lParam);
    //    [DllImport("user32.dll")]
    //    static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
    //    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    //    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    //    [DllImport("user32.dll")]
    //    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    //    [DllImport("kernel32.dll")]
    //    static extern int GetCurrentThreadId();
    //}

}

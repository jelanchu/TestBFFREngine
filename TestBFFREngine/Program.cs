using Engine_IY_RealtimeFR_BF;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestBFFREngine
{
    class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));
        static string testImageFilename = "";

        static void Main(string[] args)
        {
            testImageFilename = args[0];
            XmlConfigurator.Configure(new System.IO.FileInfo("./log4net.config"));
            //logger.Info("--- test 1 begin ---");
            //testBFFR();
            logger.Info("--- test 2 begin ---");
            testBFFR2();
        }

        static void testBFFR()
        {
            BFFREngine bffrEngien = new BFFREngine(0, true);
            for (int i = 0; i < 10; i++ )
            {
                bffrEngien.processImage(testImageFilename);
            }
            bffrEngien.Dispose();
        }

        static void testBFFR2()
        {
            BFFREngine bffrEngien = new BFFREngine(0, true);
            BFFREngine bffrEngien2 = new BFFREngine(1, true);
            logger.Info("bffrEngien: [ " + bffrEngien.GetHashCode() + " ]");
            logger.Info("bffrEngien 2nd: [ " + bffrEngien.GetHashCode() + " ]");
            logger.Info("bffrEngien2: [ " + bffrEngien2.GetHashCode() + " ]");

            if (bffrEngien.Equals(bffrEngien2))
            {
                logger.Info("bffrEngien is same with bffrEngien2.");
            }
            else
            {
                logger.Info("bffrEngien is different with bffrEngien2.");
            }

            if (bffrEngien.fdre.Equals(bffrEngien2.fdre))
            {
                logger.Info("bffrEngien.fdre is same with bffrEngien2.fdre.");
            }
            else
            {
                logger.Info("bffrEngien.fdre is different with bffrEngien2.fdre.");
                logger.Info("bffrEngien.fdre: [ " + bffrEngien.fdre.GetHashCode() + " ]");
                logger.Info("bffrEngien.fdre 2nd: [ " + bffrEngien.fdre.GetHashCode() + " ]");
                logger.Info("bffrEngien2.fdre: [ " + bffrEngien2.fdre.GetHashCode() + " ]");
            }
            ParameterizedThreadStart myPar = new ParameterizedThreadStart(bffrEngien.processImage);
            ParameterizedThreadStart myPar2 = new ParameterizedThreadStart(bffrEngien2.processImage);

            for (int i = 0; i < 10; i++ )
            {
                Thread thread = new Thread(myPar);
                thread.Start(testImageFilename);

                Thread thread2 = new Thread(myPar2);
                thread2.Start(testImageFilename);

                thread.Join();
                thread2.Join();

                //Thread.Sleep(500);
            }

            bffrEngien.Dispose();
            bffrEngien2.Dispose();
        }
    }
}

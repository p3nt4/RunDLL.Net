using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using RGiesecke.DllExport;


namespace RunDLL.Net
{


    public class utilConsole {
        [DllImport("kernel32.dll",
               EntryPoint = "GetStdHandle",
               SetLastError = true,
               CharSet = CharSet.Auto,
               CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        private const int STD_OUTPUT_HANDLE = -11;
        private const int MY_CODE_PAGE = 437;

        static public void getConsole() {
            AllocConsole();
            IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
            FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
            System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(MY_CODE_PAGE);
            StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }

        static public string[] parseArgs(string source) {

            StringBuilder currentToken = new StringBuilder();
            bool inDelimitedString = false;
            List<string> scannedTokens = new List<string>();
            bool prevSlash = false;
            foreach (char c in source)
            {
                if (prevSlash)
                {
                    currentToken.Append(c);
                    prevSlash = false;
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                            if (inDelimitedString)
                            {
                                if (currentToken.Length > 0)
                                {
                                    scannedTokens.Add(currentToken.ToString());
                                    currentToken.Clear();
                                }
                            }
                            inDelimitedString = !inDelimitedString;
                            break;
                        case '\\':
                            prevSlash = true;
                            break;
                        case ' ':
                            if (!inDelimitedString)
                            {
                                if (currentToken.Length > 0)
                                {
                                    scannedTokens.Add(currentToken.ToString());
                                    currentToken.Clear();
                                }
                            }
                            else
                            {
                                currentToken.Append(c);
                            }
                            break;
                        default:
                            currentToken.Append(c);
                            break;
                    }
                }

            }
            if (currentToken.Length > 0)
            {
                scannedTokens.Add(currentToken.ToString());
                currentToken.Clear();
            }

            return scannedTokens.ToArray();
        }

    }

    public class Program
    {


        [DllExport("main", CallingConvention = CallingConvention.Cdecl)]
        public static void main(IntPtr hwnd, IntPtr hinst, string lpszCmdLine, int nCmdShow)
        {
            Main(lpszCmdLine.Split(' '));
        }

        static byte[] DownloadFile(string url)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";

            UTF8Encoding encoding = new UTF8Encoding();

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    MemoryStream ms = new MemoryStream();
                    byte[] buffer = new byte[16384];
                    int bytesRead;
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                    }
                    return ms.ToArray();
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                utilConsole.getConsole();
                Console.WriteLine("######################   [runDll.Net]   ######################");
                
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage:");
                    Console.WriteLine("rundll32 rundll.Net.dll,main <assembly> <class> <method> [(type)][arg1] [(type)][arg2]...");
                    Console.WriteLine("");
                    Console.WriteLine("Example:");
                    Console.WriteLine("rundll32 rundll.Net.dll,main C:\\Program.dll MyProgram.Program DoThing \"Example string\" (bool)true (int)3");
                    Console.WriteLine("rundll32 rundll.Net.dll,main http://website.com/Program.dll MyProgram.Program DoThing \"Example string\" (bool)true (int)3");
                    Console.ReadLine();
                    return;
                }
                string argString = "";
                for (var i = 3; i < args.Length; i++)
                {
                    argString += " " + args[i];
                }

                string[] argStrings = utilConsole.parseArgs(argString);

                Assembly a;
                //Load the assembly
                if (args[0].StartsWith("http://") || args[0].StartsWith("https://"))
                {
                    byte[] bytes = DownloadFile(args[0]);
                    a = Assembly.Load(bytes);
                }
                else
                {
                    Console.WriteLine("[Assembly]   " + args[0]);
                    a = Assembly.LoadFile(args[0]);
                }

                // Get the type to use
                Console.WriteLine("[Class]      " + args[1]);
                Type myType = a.GetType(args[1]);

                if (myType == null) {
                    Console.WriteLine("######################      Error      ######################");
                    Console.WriteLine("The selected class does not exist in the loaded assembly or is not accessible");
                    Console.ReadLine();
                    return;
                }
                // Get the method to call
                Console.WriteLine("[Method]     " + args[2]);

                Console.WriteLine("[Arguments]");
                object[] argObjects = new object[argStrings.Length];
                Type[] argTypes = new Type[argStrings.Length];

                for (var i = 0; i < argStrings.Length; i++) {
                    Console.WriteLine("             " + argStrings[i]);
                    if (argStrings[i].StartsWith("(int)"))
                    {
                        argObjects[i] = Int32.Parse(argStrings[i].Substring(5));
                    }
                    else if (argStrings[i].StartsWith("(bool)"))
                    {
                        argObjects[i] = Boolean.Parse(argStrings[i].Substring(6));
                    }
                    else {
                        argObjects[i] = argStrings[i];
                    }
                    argTypes[i] = argObjects[i].GetType();
                }
                MethodInfo myMethod;
                try
                {
                   myMethod = myType.GetMethod(args[2]);
                }
                catch (System.Reflection.AmbiguousMatchException) {
                    myMethod = myType.GetMethod(args[2], argTypes);
                }

                if (myMethod == null)
                {
                    Console.WriteLine("######################      Error      ######################");
                    Console.WriteLine("The selected method does not exist");
                    Console.ReadLine();
                    return;
                }

                // Invoke method
                Console.WriteLine("######################  Console Output  ######################");
                var result = myMethod.Invoke(0, argObjects);

                Console.WriteLine("######################  Method Result   ######################");

                Console.WriteLine(result);

                Console.ReadLine();
            }
            catch (Exception e){
                Console.WriteLine("######################      Error      ######################");
                Console.WriteLine(e);
                Console.ReadLine();
            }

        }

    }
}

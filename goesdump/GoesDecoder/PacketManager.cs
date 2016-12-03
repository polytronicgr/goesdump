﻿using System;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace OpenSatelliteProject {
    public static class PacketManager {

        private static readonly int MAX_RUNS = 20;

        public static void ManageFile(string filename) {
            FileStream fs = File.OpenRead(filename);
            byte[] szb = new byte[3];
            fs.Read(szb, 0, 3); // Ignore header size and type

            fs.ReadByte();
            szb = new byte[4];
            fs.Read(szb, 0, 4);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(szb);
            }

            int hsize = (int) BitConverter.ToUInt32(szb, 0);

            if (hsize < 0) { // Header size should never be that big.
                return;
            }

            szb = new byte[hsize];

            fs.Seek(0, SeekOrigin.Begin);

            fs.Read(szb, 0, hsize);
            fs.Close();

            string fname = GetFilename(szb);

            if (fname == "--") {
                //Console.WriteLine("Cannot find filename for {0}", filename);
            } else {
                string dir = Path.GetDirectoryName(filename);
                UIConsole.GlobalConsole.Log(String.Format("New file {0}", fname));
                //Console.WriteLine("Renaming {0} to {1}", filename, Path.Combine(dir, fname));
                File.Move(filename, Path.Combine(dir, fname));
            }
        }

        public static string Decompressor(string prefix, int pixels, int startnum, int endnum) {
            try {
                Process decompressor = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "wine";
                startInfo.Arguments = String.Format("Decompress.exe {0} {1} {2} {3} a", prefix, pixels, startnum + 1, endnum);
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.EnvironmentVariables.Add("WINEDEBUG", "fixme-all,err-winediag");

                decompressor.StartInfo = startInfo;

                UIConsole.GlobalConsole.Debug(String.Format("Calling {0}", startInfo.Arguments));
                decompressor.Start();
                decompressor.WaitForExit();

                if (decompressor.ExitCode != 0) {
                    string stderr = decompressor.StandardError.ReadToEnd();
                    UIConsole.GlobalConsole.Error(String.Format("Error Decompressing: {0}", stderr));
                } else {
                    UIConsole.GlobalConsole.Debug(String.Format("Decompress sucessful to {0}", String.Format("{0}_decomp{1}.lrit", prefix, startnum)));
                }

            } catch (Exception e) {
                UIConsole.GlobalConsole.Error(String.Format("Error running decompressor: {0}", e));
            }


            return String.Format("{0}_decomp{1}.lrit", prefix, startnum);
        }

        public static string GetFilename(byte[] data) {
            string filename = "--";
            int runs = 0;

            if (data.Length == 0) {
                return filename;
            }
            while (true) {
                byte type = data[0];
                byte[] cb = data.Skip(1).Take(2).ToArray();

                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(cb);
                }

                UInt16 size = BitConverter.ToUInt16(cb, 0);
                if (type == 4) {
                    return System.Text.Encoding.UTF8.GetString(data.Skip(3).Take(size - 3).ToArray());
                }

                data = data.Skip(size).ToArray();
                if (data.Length == 0) {
                    break;
                }
                runs++;
                if (runs >= MAX_RUNS) {
                    break;
                }
            }

            return filename;
        }

        public static bool IsCompressed(byte[] data) {
            bool IsCompressed = false;
            int runs = 0;

            while (true) {
                byte type = data[0];
                byte[] cb = data.Skip(1).Take(2).ToArray();

                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(cb);
                }

                UInt16 size = BitConverter.ToUInt16(cb, 0);
                if (type == 129) {
                    //4sHHHB
                    return data[13] > 0;
                } else if (type == 1) {
                    //>BHHB
                    return data[5] > 0;
                }

                data = data.Skip(size).ToArray();
                if (data.Length == 0) {
                    break;
                }

                runs++;
                if (runs >= MAX_RUNS) {
                    break;
                }
            }

            return IsCompressed;
        }

        public static int GetPixels(byte[] data) {
            int pixels = 0;
            int runs = 0;
            while (true) {
                byte type = data[0];
                byte[] cb = data.Skip(1).Take(2).ToArray();

                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(cb);
                }

                UInt16 size = BitConverter.ToUInt16(cb, 0);
                if (type == 1) {
                    //>BHHB
                    cb = data.Skip(4).Take(2).ToArray();
                    if (BitConverter.IsLittleEndian) {
                        Array.Reverse(cb);
                    }

                    return BitConverter.ToUInt16(cb, 0);
                }

                data = data.Skip(size).ToArray();
                if (data.Length == 0) {
                    break;
                }
                runs++;
                if (runs >= MAX_RUNS) {
                    break;
                }
            }
            return pixels;
        }
    }
}


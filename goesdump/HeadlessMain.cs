﻿using System;
using System.Threading;
using WebSocketSharp.Server;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Concurrent;
using WebSocketSharp;
using System.Collections.Generic;
using System.Linq;
using OpenSatelliteProject.Tools;
using OpenSatelliteProject.PacketData.Enums;

namespace OpenSatelliteProject {
    public class HeadlessMain {

        private static readonly int MAX_CACHED_MESSAGES = 10;

        private ImageManager FDImageManager;
        private ImageManager XXImageManager;
        private ImageManager NHImageManager;
        private ImageManager SHImageManager;
        private ImageManager USImageManager;

        private Mutex mtx;
        private Connector cn;
        private DemuxManager demuxManager;
        private Statistics_st statistics;
        private StatisticsModel stModel;
        private HttpServer httpsv;

        private static List<ConsoleMessage> messageList = new List<ConsoleMessage>();
        private static Mutex messageListMutex = new Mutex();

        private bool running = false;

        public static List<ConsoleMessage> GetCachedMessages {
            get {
                List<ConsoleMessage> tmp;
                messageListMutex.WaitOne();
                tmp = messageList.Clone<ConsoleMessage>();
                messageListMutex.ReleaseMutex();
                return tmp;
            }
        }

        public HeadlessMain() {
            
            string fdFolder = PacketManager.GetFolderByProduct(NOAAProductID.SCANNER_DATA_1, (int)ScannerSubProduct.INFRARED_FULLDISK);
            string xxFolder = PacketManager.GetFolderByProduct(NOAAProductID.SCANNER_DATA_1, (int)ScannerSubProduct.INFRARED_AREA_OF_INTEREST);
            string nhFolder = PacketManager.GetFolderByProduct(NOAAProductID.SCANNER_DATA_1, (int)ScannerSubProduct.INFRARED_NORTHERN);
            string shFolder = PacketManager.GetFolderByProduct(NOAAProductID.SCANNER_DATA_1, (int)ScannerSubProduct.INFRARED_SOUTHERN);
            string usFolder = PacketManager.GetFolderByProduct(NOAAProductID.SCANNER_DATA_1, (int)ScannerSubProduct.INFRARED_UNITEDSTATES);
            FDImageManager = new ImageManager(Path.Combine("channels", fdFolder));
            XXImageManager = new ImageManager(Path.Combine("channels", xxFolder));
            NHImageManager = new ImageManager(Path.Combine("channels", nhFolder));
            SHImageManager = new ImageManager(Path.Combine("channels", shFolder));
            USImageManager = new ImageManager(Path.Combine("channels", usFolder));

            mtx = new Mutex();
            cn = new Connector();            
            demuxManager = new DemuxManager();
            cn.StatisticsAvailable += (Statistics_st data) => {
                mtx.WaitOne();
                statistics = data;
                mtx.ReleaseMutex();

                stModel.Refresh(statistics);
                httpsv.WebSocketServices.Broadcast(stModel.toJSON());
            };

            cn.ChannelDataAvailable += (byte[] data) => demuxManager.parseBytes(data);
            cn.ConstellationDataAvailable += (float[] data) => {
                ConstellationModel cm = new ConstellationModel(data);
                if (httpsv.IsListening) {
                    httpsv.WebSocketServices.Broadcast(cm.toJSON());
                }
            };

            statistics = new Statistics_st();
            stModel = new StatisticsModel(statistics);
            UIConsole.GlobalConsole.Log("Headless Main Created");
            httpsv = new HttpServer(8090);

            httpsv.RootPath = Path.Combine(".", "web");
            httpsv.OnGet += HandleHTTPGet;

            httpsv.AddWebSocketService<WSHandler>("/mainws");

            UIConsole.GlobalConsole.MessageAvailable += (data) => {
                ConsoleModel cm = new ConsoleModel(data.Priority.ToString(), data.Message);
                if (httpsv.IsListening) {
                    httpsv.WebSocketServices["/mainws"].Sessions.Broadcast(cm.toJSON());
                }

                messageListMutex.WaitOne();
                if (messageList.Count >= MAX_CACHED_MESSAGES) {
                    messageList.RemoveAt(0);
                }
                messageList.Add(data);
                messageListMutex.ReleaseMutex();
            };
        }

        private void HandleHTTPGet(object sender, HttpRequestEventArgs e) {
            var req = e.Request;
            var res = e.Response;

            var path = req.RawUrl;
            if (path == "/")
                path += "index.html";

            var content = httpsv.GetFile(path);
            if (content == null) {
                res.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            if (path.EndsWith(".html")) {
                res.ContentType = "text/html";
                res.ContentEncoding = Encoding.UTF8;
            } else if (path.EndsWith(".js")) {
                res.ContentType = "application/javascript";
                res.ContentEncoding = Encoding.UTF8;
            }

            res.WriteContent(content);
        }

        public void Start() {
            UIConsole.GlobalConsole.Log("Headless Main Starting");
            FDImageManager.Start();
            XXImageManager.Start();
            NHImageManager.Start();
            SHImageManager.Start();
            USImageManager.Start();

            cn.Start();
            httpsv.Start();
            running = true;
            while (running) {
                Thread.Sleep(10);
            }
            httpsv.Stop();
        }
    }
}


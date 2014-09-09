using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Text;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer
{
    public class ScreenshotCache
    {
        private string screenshotDirectory
        {
            get
            {
                if (Settings.settingsStore.screenshotDirectory != "")
                {
                    return Settings.settingsStore.screenshotDirectory;
                }
                return Path.Combine(Server.universeDirectory, "Screenshots");
            }
        }

        public string[] GetCachedObjects()
        {
            string[] cacheFiles = Directory.GetFiles(screenshotDirectory);
            string[] cacheObjects = new string[cacheFiles.Length];
            for (int i = 0; i < cacheFiles.Length; i++)
            {
                cacheObjects[i] = Path.GetFileNameWithoutExtension(cacheFiles[i]);
            }
            return cacheObjects;
        }

        public void ExpireCache()
        {
            string[] cacheObjects = GetCachedObjects();
            foreach (string cacheObject in cacheObjects)
            {
                string cacheFile = Path.Combine(screenshotDirectory, cacheObject + ".png");
                //If the file is older than a day, delete it
                if (File.GetCreationTime(cacheFile).AddDays(1d) < DateTime.Now)
                {
                    DarkLog.Debug("Deleting saved screenshot " + cacheObject + ", reason: Expired!");
                    File.Delete(cacheFile);
                }
            }
        }
    }
}

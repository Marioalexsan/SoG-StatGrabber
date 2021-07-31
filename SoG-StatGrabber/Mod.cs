using SoG.Modding.Core;
using SoG.StatGrabber.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using SoG.Modding.Content;
using SoG.StatGrabber.Extractors;

namespace SoG.StatGrabber
{
    public class StatGrabberTool : BaseScript
    {
        public override void LoadContent()
        {
            ItemDataExtractor.Logger = Logger;
            ModAPI.MiscAPI.CreateCommand(
                "DumpItemData",
                ItemDataExtractor.Extract
                );
        }
    }
}

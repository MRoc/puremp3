using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Processor;
using ID3;
using CoreLogging;
using System.Net;
using System.IO;
using CoreUtils;
using System.Web;
using System.Diagnostics;
using ID3CoverSearch;
using CoreVirtualDrive;
using ID3.Utils;

namespace PureMp3.Model.Batch
{
    public class CoverSearchProcessor : IProcessorMutable
    {
        public CoverSearchProcessor()
        {
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(Tag) };
        }
        public void Process(object obj)
        {
            Logger.WriteLine(Tokens.Info, "Cover search...");

            Tag tag = obj as Tag;
            
            try
            {
                GoogleImageQuery.ImageResult result = query.Query(BuildQuery(tag));

                if (!Object.ReferenceEquals(result, null))
                {
                    WriteCoverToDisc(tag, result);
                    CreatePictureTag(tag, result.Image, result.Url);
                }
                else
                {
                    Logger.WriteLine(Tokens.Info, "No results found!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(Tokens.Exception, ex);
            }
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }

        private string BuildQuery(Tag tag)
        {
            TagEditor editor = new TagEditor(tag);

            StringBuilder result = new StringBuilder();

            result.Append(editor.Artist);
            result.Append(" ");
            result.Append(editor.Album);

            return result.ToString();
        }

        private void CreatePictureTag(Tag tag, byte[] image, string url)
        {
            FrameDescription frameDesc = tag.DescriptionMap[FrameMeaning.Picture];

            if (Object.ReferenceEquals(frameDesc, null))
            {
                // TODO
                Logger.WriteLine(Tokens.Info, "Tag version does not support images!");

                return;
            }

            Logger.WriteLine(Tokens.InfoVerbose, "  Creating album art frame...");

            tag.Frames
                .Where(n => n.FrameId == frameDesc.FrameId).ToArray()
                .ForEach(n => tag.Remove(n));

            TagEditor editor = new TagEditor(tag);
            editor.Picture = image;
        }

        private GoogleImageQuery query = new GoogleImageQuery();

        [Conditional("DEBUG")]
        private void WriteCoverToDisc(Tag tag, GoogleImageQuery.ImageResult result)
        {
            string cachePath = Path.Combine(App.AppDataFolder, "Covers");
            if (!VirtualDrive.ExistsDirectory(cachePath))
            {
                // TODO: VirtualDrive
                VirtualDrive.CreateDirectory(cachePath);
            }

            TagEditor editor = new TagEditor(tag);
            string url = result.Url;
            string suffix = url.Substring(url.Length - 3, 3);
            string file = Path.Combine(cachePath, dirNameGenerator.Name(editor.Get()) + "." + suffix);

            if (!File.Exists(file))
            {
                using (Stream stream = File.OpenWrite(file))
                {
                    stream.Write(result.Image, 0, result.Image.Length);
                }
            }
        }

        private DirectoryNameGenerator dirNameGenerator = new DirectoryNameGenerator();
    }    
}

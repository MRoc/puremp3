using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CoreLogging;
using ID3WebQueryBase;

namespace ID3Freedb
{
    public class FreedbAccess
    {
        public static Release QueryRelease(
            IEnumerable<int> lengths,
            DirectoryInfo dirInfo,
            MultipleItemChooser.MultipleChoiseHeuristic heuristic)
        {
            FreedbAPI freedb = new FreedbAPI();

            IEnumerable<int> offsets = DiscID.MakeOffsets(lengths);
            uint discid = DiscID.DiscId(offsets);
            string freeDbQuery = DiscID.FreedbQuery(discid, offsets);

            var result = freedb.Query(freeDbQuery);

            if (result.Code == FreedbAPI.ResponseCodes.CODE_200_ExactMatchFound
                || result.Code == FreedbAPI.ResponseCodes.CODE_211_InexactMatchFoundListFollows
                || result.Code == FreedbAPI.ResponseCodes.CODE_210_OkOrMultipleExactMatches)
            {
                MultipleItemChooser chooser = new MultipleItemChooser(dirInfo.Name, discid, result.Value, heuristic);
                Release preferred = chooser.ChooseQuery();

                bool found = !Object.ReferenceEquals(preferred, null);

                if (!found && result.Value.Count() > 0)
                {
                    LoggerWriter.WriteLine(Tokens.Info, "Multiple matches found but heuristic could not decide,");
                    LoggerWriter.WriteLine(Tokens.Info, "it might help to set the heuristic to fuzzy or rename");
                    LoggerWriter.WriteLine(Tokens.Info, "the folders so that they contain the artist name.");
                }

                if (found)
                {
                    FreedbAPI.Result<Release> disc = freedb.Read(preferred);

                    if (disc.Code == FreedbAPI.ResponseCodes.CODE_210_OkOrMultipleExactMatches)
                    {
                        return disc.Value;
                    }
                }
            }

            return null;
        }
    }
}

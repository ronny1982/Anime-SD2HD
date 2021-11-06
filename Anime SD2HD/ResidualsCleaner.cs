using System.IO;

namespace AnimeSD2HD
{
    internal interface IResidualsCleaner
    {
        void Cleanup(params string[] paths);
    }

    internal class ResidualsCleaner : IResidualsCleaner
    {
        public void Cleanup(params string[] paths)
        {
            foreach(var path in paths)
            {
                Delete(path);
            }
        }

        private static void Delete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                // TODO: error handling ...
            }
        }
    }
}
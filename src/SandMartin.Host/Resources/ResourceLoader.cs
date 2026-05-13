using System;
using System.Drawing;
using System.Reflection;

namespace SandMartin.Host.Resources
{
    public static class ResourceLoader
    {
        private static Bitmap _icon;

        public static Bitmap SandMartinIcon
        {
            get
            {
                if (_icon == null)
                {
                    try
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        using (var stream = assembly.GetManifestResourceStream("SandMartin.Host.Resources.SandMartinIcon.png"))
                        {
                            if (stream != null)
                            {
                                _icon = new Bitmap(stream);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
                return _icon;
            }
        }
    }
}

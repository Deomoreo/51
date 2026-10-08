using System.IO;
using System.Text;
using NUnit.Framework;

namespace Project51.Tests
{
    /// <summary>
    /// Build 3 (I2): un .cs salvato in Windows-1252 compila bene su Windows ma la Cloud Build (Mac) perde gli accenti dei testi
    /// ("Questa email è già in uso" a quadratini). Tutti i sorgenti del progetto devono essere UTF-8.
    /// </summary>
    public class SourceEncodingTests
    {
        [Test]
        public void ProjectSourcesAreUtf8()
        {
            var strict = new UTF8Encoding(false, true);
            var bad = new StringBuilder();
            foreach (var root in new[] { "Assets/Scripts", "Assets/UIV2", "Assets/UI51", "Assets/Editor", "Assets/Tests" })
            {
                if (!Directory.Exists(root)) continue;
                foreach (var path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    try { strict.GetString(File.ReadAllBytes(path)); }
                    catch (DecoderFallbackException) { bad.AppendLine(path); }
                }
            }
            Assert.IsEmpty(bad.ToString(), "File non UTF-8 (ricodificarli):\n" + bad);
        }
    }
}

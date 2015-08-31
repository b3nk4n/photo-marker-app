using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Windows;

namespace PhotoNote.Test
{
    [TestClass]
    public class TextContextTest
    {
        [TestMethod]
        public void TestSerielizeFontFamily()
        {
            var textContext = new TextContext();
            textContext.Weight = FontWeights.Bold;
            var jsonTextContext = JsonConvert.SerializeObject(textContext);
            var deserializedContext = JsonConvert.DeserializeObject<TextContext>(jsonTextContext);

            Assert.AreEqual(textContext.Weight, deserializedContext.Weight);
        }
    }
}

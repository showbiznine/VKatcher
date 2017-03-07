namespace VK.WindowsPhone.SDK.API.Model
{
    public class VKTag
    {
        public string tag { get; set; }

        public string domain { get; set; }
        public bool IsSubscribed { get; set; }
        public long LastSavedId { get; set; }
        public int to_save { get; set; }
    }
}
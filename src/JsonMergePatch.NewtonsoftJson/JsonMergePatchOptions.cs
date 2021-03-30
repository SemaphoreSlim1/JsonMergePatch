namespace JsonMergePatch
{
    public class JsonMergePatchOptions
    {
        public static JsonMergePatchOptions Default { get; } = new JsonMergePatchOptions();
        
        /// <summary>
        /// If true, A value of null means delete. Otherwise, a value of null means set to null.
        /// </summary>
        public bool EnableDelete { get; set; }
    }
}

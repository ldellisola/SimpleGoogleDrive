namespace SimpleGoogleDrive
{

    internal class StringValueAttribute : Attribute
    {
        public string value;

        public StringValueAttribute(string value)
        {
            this.value = value;
        }
    }

}

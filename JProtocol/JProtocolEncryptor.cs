namespace JProtocol
{
    public static class JProtocolEncryptor
    {
        private static string Key => "swintuuuus";

        public static byte[] Encrypt(byte[] data) => RijndaelHandler.Encrypt(data, Key);

        public static byte[] Decrypt(byte[] data) => RijndaelHandler.Decrypt(data, Key);
    }
}
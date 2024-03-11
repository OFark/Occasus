/*
 * This work (Modern Encryption of a String C#, by James Tuley), 
 * identified by James Tuley, is free of known copyright restrictions.
 * https://gist.github.com/4336842
 * http://creativecommons.org/publicdomain/mark/1.0/ 
 */

using MudBlazor;
using System.Security.Cryptography;
using System.Text;

namespace Occasus.Helpers;

public class AESThenHMAC
{
    private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();
    private readonly static byte[] Header = Encoding.UTF8.GetBytes("ENCRYPTED_WITH_AES");

    //Preconfigured Encryption Parameters
    public static readonly int BlockBitSize = 128;
    public static readonly int KeyBitSize = 256;

    //Preconfigured Password Key Derivation Parameters
    public static readonly int SaltBitSize = 64;
    public static readonly int Iterations = 10000;
    public static readonly int MinPasswordLength = 12;

    /// <summary>
    /// Helper that generates a random key on each call.
    /// </summary>
    /// <returns></returns>
    public static byte[] NewKey()
    {
        var key = new byte[KeyBitSize / 8];
        Random.GetBytes(key);
        return key;
    }

    /// <summary>
    /// Simple Encryption (AES) then Authentication (HMAC) for a UTF8 Message.
    /// </summary>
    /// <param name="secretMessage">The secret message.</param>
    /// <param name="cryptKey">The crypt key.</param>
    /// <param name="authKey">The auth key.</param>
    /// <param name="nonSecretPayload">(Optional) Non-Secret Payload.</param>
    /// <returns>
    /// Encrypted Message
    /// </returns>
    /// <exception cref="System.ArgumentException">Secret Message Required!;secretMessage</exception>
    /// <remarks>
    /// Adds overhead of (Optional-Payload + BlockSize(16) + Message-Padded-To-Blocksize +  HMac-Tag(32)) * 1.33 Base64
    /// </remarks>
    public static string SimpleEncrypt(string secretMessage, byte[] cryptKey, byte[] authKey, byte[]? nonSecretPayload = null)
    {
        if (string.IsNullOrEmpty(secretMessage))
            throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

        var plainText = Encoding.UTF8.GetBytes(secretMessage);
        var cipherText = SimpleEncrypt(plainText, cryptKey, authKey, nonSecretPayload);
        return Convert.ToBase64String(cipherText);
    }

    /// <summary>
    /// Simple Authentication (HMAC) then Decryption (AES) for a secrets UTF8 Message.
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message.</param>
    /// <param name="cryptKey">The crypt key.</param>
    /// <param name="authKey">The auth key.</param>
    /// <param name="nonSecretPayloadLength">Length of the non secret payload.</param>
    /// <returns>
    /// Decrypted Message
    /// </returns>
    /// <exception cref="System.ArgumentException">Encrypted Message Required!;encryptedMessage</exception>
    public static string? SimpleDecrypt(string encryptedMessage, byte[] cryptKey, byte[] authKey,int nonSecretPayloadLength = 0)
    {
        if (string.IsNullOrWhiteSpace(encryptedMessage))
            throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

        var cipherText = Convert.FromBase64String(encryptedMessage);
        var plainText = SimpleDecrypt(cipherText, cryptKey, authKey, nonSecretPayloadLength);
        return plainText == null ? null : Encoding.UTF8.GetString(plainText);
    }

    /// <summary>
    /// Simple Encryption (AES) then Authentication (HMAC) of a UTF8 message
    /// using Keys derived from a Password (PBKDF2).
    /// </summary>
    /// <param name="secretMessage">The secret message.</param>
    /// <param name="password">The password.</param>
    /// <param name="nonSecretPayload">The non secret payload.</param>
    /// <returns>
    /// Encrypted Message
    /// </returns>
    /// <exception cref="System.ArgumentException">password</exception>
    /// <remarks>
    /// Significantly less secure than using random binary keys.
    /// Adds additional non secret payload for key generation parameters.
    /// </remarks>
    public static string SimpleEncryptWithPassword(string secretMessage, string password, byte[]? nonSecretPayload = null)
    {
        if (string.IsNullOrEmpty(secretMessage))
            throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

        var plainText = Encoding.UTF8.GetBytes(secretMessage);
        var cipherText = SimpleEncryptWithPassword(plainText, password, nonSecretPayload);
        return Convert.ToBase64String(cipherText);
    }

    /// <summary>
    /// Simple Authentication (HMAC) and then Descryption (AES) of a UTF8 Message
    /// using keys derived from a password (PBKDF2). 
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message.</param>
    /// <param name="password">The password.</param>
    /// <param name="nonSecretPayloadLength">Length of the non secret payload.</param>
    /// <returns>
    /// Decrypted Message
    /// </returns>
    /// <exception cref="System.ArgumentException">Encrypted Message Required!;encryptedMessage</exception>
    /// <remarks>
    /// Significantly less secure than using random binary keys.
    /// </remarks>
    public static string? SimpleDecryptWithPassword(string encryptedMessage, string password, int nonSecretPayloadLength = 0)
    {
        if (string.IsNullOrWhiteSpace(encryptedMessage))
            throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

        var cipherText = Convert.FromBase64String(encryptedMessage);
        var plainText = SimpleDecryptWithPassword(cipherText, password, nonSecretPayloadLength);
        return plainText == null ? null : Encoding.UTF8.GetString(plainText);
    }

    /// <summary>
    /// Simple Encryption(AES) then Authentication (HMAC) for a UTF8 Message.
    /// </summary>
    /// <param name="secretMessage">The secret message.</param>
    /// <param name="cryptKey">The crypt key.</param>
    /// <param name="authKey">The auth key.</param>
    /// <param name="nonSecretPayload">(Optional) Non-Secret Payload.</param>
    /// <returns>
    /// Encrypted Message
    /// </returns>
    /// <remarks>
    /// Adds overhead of (Optional-Payload + BlockSize(16) + Message-Padded-To-Blocksize +  HMac-Tag(32)) * 1.33 Base64
    /// </remarks>
    public static byte[] SimpleEncrypt(byte[] secretMessage, byte[] cryptKey, byte[] authKey, byte[]? nonSecretPayload = null)
    {
        //User Error Checks
        if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
            throw new ArgumentException(string.Format("Key needs to be {0} bit!", KeyBitSize), nameof(cryptKey));

        if (authKey == null || authKey.Length != KeyBitSize / 8)
            throw new ArgumentException(string.Format("Key needs to be {0} bit!", KeyBitSize), nameof(authKey));

        if (secretMessage == null || secretMessage.Length < 1)
            throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

        //non-secret payload optional
        nonSecretPayload ??= [];

        byte[] cipherText;
        byte[] iv;

        using (var aes = Aes.Create())
        {

            aes.KeySize = KeyBitSize;
            aes.BlockSize = BlockBitSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
        

            //Use random IV
            aes.GenerateIV();
            iv = aes.IV;

            using var encrypter = aes.CreateEncryptor(cryptKey, iv);
            using var cipherStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
            using (var binaryWriter = new BinaryWriter(cryptoStream))
            {
                //Encrypt Data
                binaryWriter.Write(secretMessage);
            }

            cipherText = cipherStream.ToArray();

        }

        //Assemble encrypted message and add authentication
        using var hmac = new HMACSHA256(authKey);
        using var encryptedStream = new MemoryStream();
        using (var binaryWriter = new BinaryWriter(encryptedStream))
        {
            //Prepend non-secret payload if any
            binaryWriter.Write(nonSecretPayload);
            //Prepend IV
            binaryWriter.Write(iv);
            //Write Ciphertext
            binaryWriter.Write(cipherText);
            binaryWriter.Flush();

            //Authenticate all data
            var tag = hmac.ComputeHash(encryptedStream.ToArray());
            //Postpend tag
            binaryWriter.Write(tag);
            binaryWriter.Flush();
        }
        return encryptedStream.ToArray();

    }

    /// <summary>
    /// Simple Authentication (HMAC) then Decryption (AES) for a secrets UTF8 Message.
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message.</param>
    /// <param name="cryptKey">The crypt key.</param>
    /// <param name="authKey">The auth key.</param>
    /// <param name="nonSecretPayloadLength">Length of the non secret payload.</param>
    /// <returns>Decrypted Message</returns>
    public static byte[]? SimpleDecrypt(byte[] encryptedMessage, byte[] cryptKey, byte[] authKey, int nonSecretPayloadLength = 0)
    {

        //Basic Usage Error Checks
        if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
            throw new ArgumentException(string.Format("CryptKey needs to be {0} bit!", KeyBitSize), nameof(cryptKey));

        if (authKey == null || authKey.Length != KeyBitSize / 8)
            throw new ArgumentException(string.Format("AuthKey needs to be {0} bit!", KeyBitSize), nameof(authKey));

        if (encryptedMessage == null || encryptedMessage.Length == 0)
            throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

        using var hmac = new HMACSHA256(authKey);
        var sentTag = new byte[hmac.HashSize / 8];
        //Calculate Tag
        var calcTag = hmac.ComputeHash(encryptedMessage, 0, encryptedMessage.Length - sentTag.Length);
        var ivLength = (BlockBitSize / 8);

        //if message length is to small just return null
        if (encryptedMessage.Length < sentTag.Length + nonSecretPayloadLength + ivLength)
            return null;

        //Grab Sent Tag
        Array.Copy(encryptedMessage, encryptedMessage.Length - sentTag.Length, sentTag, 0, sentTag.Length);

        //Compare Tag with constant time comparison
        var compare = 0;
        for (var i = 0; i < sentTag.Length; i++)
            compare |= sentTag[i] ^ calcTag[i];

        //if message doesn't authenticate return null
        if (compare != 0)
            return null;

        using var aes = Aes.Create();

        aes.KeySize = KeyBitSize;
        aes.BlockSize = BlockBitSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        //Grab IV from message
        var iv = new byte[ivLength];
        Array.Copy(encryptedMessage, nonSecretPayloadLength, iv, 0, iv.Length);

        using var decrypter = aes.CreateDecryptor(cryptKey, iv);
        using var plainTextStream = new MemoryStream();
        using (var decrypterStream = new CryptoStream(plainTextStream, decrypter, CryptoStreamMode.Write))
        using (var binaryWriter = new BinaryWriter(decrypterStream))
        {
            //Decrypt Cipher Text from Message
            binaryWriter.Write(
                encryptedMessage,
                nonSecretPayloadLength + iv.Length,
                encryptedMessage.Length - nonSecretPayloadLength - iv.Length - sentTag.Length
            );
        }
        //Return Plain Text
        return plainTextStream.ToArray();
    }

    /// <summary>
    /// Simple Encryption (AES) then Authentication (HMAC) of a UTF8 message
    /// using Keys derived from a Password (SHA256)
    /// </summary>
    /// <param name="secretMessage">The secret message.</param>
    /// <param name="password">The password.</param>
    /// <param name="nonSecretPayload">The non secret payload.</param>
    /// <returns>
    /// Encrypted Message
    /// </returns>
    /// <exception cref="System.ArgumentException">Must have a password of minimum length;password</exception>
    /// <remarks>
    /// Significantly less secure than using random binary keys.
    /// Adds additional non secret payload for key generation parameters.
    /// </remarks>
    public static byte[] SimpleEncryptWithPassword(byte[] secretMessage, string password, byte[]? nonSecretPayload = null)
    {
        nonSecretPayload ??= [];

        // User Error Checks
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new ArgumentException(string.Format("Must have a password of at least {0} characters!", MinPasswordLength), nameof(password));

        if (secretMessage == null || secretMessage.Length == 0)
            throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

        // Generate Salt
        byte[] cryptSalt = GenerateSalt();
        byte[] authSalt = GenerateSalt();

        // Derive Keys using SHA256
        byte[] cryptKey = DeriveKey(password, cryptSalt);
        byte[] authKey = DeriveKey(password, authSalt);

        // Create Non-Secret Payload
        byte[] payload = CreatePayload(nonSecretPayload, cryptSalt, authSalt);

        // Encrypt the message
        return SimpleEncrypt(secretMessage, cryptKey, authKey, payload);
    }

    private static byte[] GenerateSalt()
    {
        // Generate a random salt
        byte[] salt = new byte[SaltBitSize / 8];
        Random.GetBytes(salt);
        return salt;
    }


    private static byte[] DeriveKey(string password, byte[] salt) =>
        // Derive key using SHA256
        SHA256.HashData(Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt)));

    private static byte[] CreatePayload(byte[] nonSecretPayload, byte[] cryptSalt, byte[] authSalt)
    {
        return Header.ConcatArray(nonSecretPayload).ConcatArray(cryptSalt).ConcatArray(authSalt);
    }

    private static (byte[] Header, byte[] NonSecretPayload, byte[] CryptSalt, byte[] AuthSalt) ExtractPayload(byte[] encryptedMessage, int nonSecretPayloadLength = 0)
    {
        var arrayReader = new ArrayReader<byte>(encryptedMessage);

        var header = arrayReader.Read(Header.Length);
        var nonSecretMesasge = arrayReader.Read(nonSecretPayloadLength);
        var cryptSalt = arrayReader.Read(SaltBitSize / 8);
        var authSalt = arrayReader.Read(SaltBitSize / 8);

        return (header.ToArray(), nonSecretMesasge.ToArray(), cryptSalt.ToArray(), authSalt.ToArray());
    }

    /// <summary>
    /// Simple Authentication (HMAC) and then Descryption (AES) of a UTF8 Message
    /// using keys derived from a password (SHA256). 
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message.</param>
    /// <param name="password">The password.</param>
    /// <param name="nonSecretPayloadLength">Length of the non secret payload.</param>
    /// <returns>
    /// Decrypted Message
    /// </returns>
    /// <exception cref="System.ArgumentException">Must have a password of minimum length;password</exception>
    /// <remarks>
    /// Significantly less secure than using random binary keys.
    /// </remarks>
    public static byte[]? SimpleDecryptWithPassword(byte[] encryptedMessage, string password, int nonSecretPayloadLength = 0)
    {
        // User Error Checks
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new ArgumentException(string.Format("Must have a password of at least {0} characters!", MinPasswordLength), nameof(password));

        if (encryptedMessage == null || encryptedMessage.Length == 0)
            throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));


        if (encryptedMessage.Length < Header.Length)
        {
            return null;
        }

        var (header, _, cryptSalt, authSalt) = ExtractPayload(encryptedMessage, nonSecretPayloadLength);

        
        if (!header.SequenceEqual(Header))
        {
            return null;
        }

        // Extract salts from the encrypted message
        // Derive keys using SHA256
        byte[] cryptKey = DeriveKey(password, cryptSalt);
        byte[] authKey = DeriveKey(password, authSalt);

        // Decrypt the message
        return SimpleDecrypt(encryptedMessage, cryptKey, authKey, Header.Length + cryptSalt.Length + authSalt.Length + nonSecretPayloadLength);
    }
}

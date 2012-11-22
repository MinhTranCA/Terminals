﻿using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Terminals.Security;

namespace Tests
{
    /// <summary>
    /// Tests to ensure that passwords in version after v2.0 work
    /// and upgrade doesn't break any stored passwords
    /// </summary>
    [TestClass]
    public class PasswordTests
    {
        private const string USERPASSWORD = "testPassword";
        private const string MASTERPASSWORD = "testMaster";

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void V1MasterPasswordValidationTest()
        {
            // cant use persistence security here, because of initialization depends on newest algorithm
            string masterHash = PasswordFunctions.ComputeMasterPasswordHash(MASTERPASSWORD);
            string masterHash2 = PasswordFunctions.ComputeMasterPasswordHash(MASTERPASSWORD);
            Assert.AreEqual(masterHash, masterHash2);

            bool isValid = PasswordFunctions.MasterPasswordIsValid(MASTERPASSWORD, masterHash);
            Assert.IsTrue(isValid, "Couldn't validate stored master password v1");
        }

        [TestMethod]
        public void V1MasterPasswordIsUniqueTest()
        {
            string key1 = PasswordFunctions.CalculateMasterPasswordKey(MASTERPASSWORD);
            string key2 = PasswordFunctions.CalculateMasterPasswordKey(MASTERPASSWORD);
            Assert.AreEqual(key1, key2, "generated master password key v1 doesn't equals.");
        }

        [TestMethod]
        public void V1PasswordsUniqueEncryptionTest()
        {
            string key = PasswordFunctions.CalculateMasterPasswordKey(MASTERPASSWORD);
            string encryptedPassword = PasswordFunctions.EncryptPassword(USERPASSWORD, key);
            string encryptedPassword2 = PasswordFunctions.EncryptPassword(USERPASSWORD, key);
            Assert.AreEqual(encryptedPassword, encryptedPassword2, "password encryption v1 doesn't generate identical encrypted bytes");
        }

        [TestMethod]
        public void V1PasswordsEncryptDecryptTest()
        {
            PasswordsEncryptDecryptCheck(CheckEncryptDecryptPasswordV1);

            string masterKey = PasswordFunctions.CalculateMasterPasswordKey(MASTERPASSWORD);
            string decryptedPassword = PasswordFunctions.DecryptPassword(string.Empty, masterKey);
            Assert.AreEqual(string.Empty, decryptedPassword, "Decryption of empty stored password failed");
        }

        private static string CheckEncryptDecryptPasswordV1(string password, string masterPassword)
        {
            string masterKey = PasswordFunctions.CalculateMasterPasswordKey(masterPassword);
            string encryptedPassword = PasswordFunctions.EncryptPassword(password, masterKey);
            string decryptedPassword = PasswordFunctions.DecryptPassword(encryptedPassword, masterKey);
            Assert.AreEqual(password, decryptedPassword, "Unable to decrypt V1 password");
            return encryptedPassword;
        }

        [TestMethod]
        public void V2MasterPasswordValidationTest()
        {
            string stored = PasswordFunctions2.ComputeMasterPasswordStoredKey(MASTERPASSWORD);
            string stored2 = PasswordFunctions2.ComputeMasterPasswordStoredKey(MASTERPASSWORD);
            Assert.AreNotEqual(stored, stored2);

            bool isValid = PasswordFunctions2.MasterPasswordIsValid(MASTERPASSWORD, stored);
            Assert.IsTrue(isValid, "Unable to validate encrypted master password version 2");
        }

        [TestMethod]
        public void V2MasterPasswordIsUniqueTest()
        {
            string key1 = PasswordFunctions2.CalculateMasterPasswordKey(MASTERPASSWORD);
            string key2 = PasswordFunctions2.CalculateMasterPasswordKey(MASTERPASSWORD);
            Assert.AreNotEqual(key1, key2, "generated master password key always equals.");

            var accessor = new PrivateType(typeof(PasswordFunctions2));
            byte[] keySalt = (byte[])accessor.InvokeStatic("CreateRandomKeySalt", new Object[] {});
            key1 = CalculateV2Key(accessor, keySalt);
            key2 = CalculateV2Key(accessor, keySalt);
            Assert.AreEqual(key1, key2, "generated master password doesn't equal.");
        }

        private static string CalculateV2Key(PrivateType accessor, byte[] keySalt)
        {
            object[] parameters = new object[] {MASTERPASSWORD, keySalt};
            return accessor.InvokeStatic("CalculateMasterPasswordKey", parameters).ToString();
        }

        [TestMethod]
        public void V2PasswordsUniqueEncryptionTest()
        {
            string key = PasswordFunctions2.CalculateMasterPasswordKey(MASTERPASSWORD);
            string encryptedPassword = PasswordFunctions2.EncryptPassword(USERPASSWORD, key);
            string encryptedPassword2 = PasswordFunctions2.EncryptPassword(USERPASSWORD, key);
            Assert.AreNotEqual(encryptedPassword, encryptedPassword2, "password encryption always generates identical encrypted bytes");
        }

        [TestMethod]
        public void V2UpgradePasswordsTest()
        {
            string targetDir = Path.Combine(this.TestContext.TestDir, "Terminals.config");
            throw new NotImplementedException();
            // deploy test version 2.0 config file
            // check, if we are able to read all passwords (validate using old version)
            // use upgrade routine to ensure, that we are able to:
            // - update the stored master password
            // - all stored passwords in favorite, credentials, RDP protocol properties are still valid
        }

        [TestMethod]
        public void V2PasswordsEncryptDecryptTest()
        {
            PasswordsEncryptDecryptCheck(CheckEncryptDecryptPasswordV2);

            string masterKey = PasswordFunctions2.CalculateMasterPasswordKey(MASTERPASSWORD);
            string decryptedPassword = PasswordFunctions2.DecryptPassword(string.Empty, masterKey);
            Assert.AreEqual(string.Empty, decryptedPassword, "Decryption of empty stored password failed");
        }

        private static string CheckEncryptDecryptPasswordV2(string password, string masterPassword)
        {
            string masterKey = PasswordFunctions2.CalculateMasterPasswordKey(masterPassword);
            string encryptedPassword = PasswordFunctions2.EncryptPassword(password, masterKey);
            string decryptedPassword = PasswordFunctions2.DecryptPassword(encryptedPassword, masterKey);
            Assert.AreEqual(password, decryptedPassword, "Unable to decrypt password");
            return encryptedPassword;
        }

        private static void PasswordsEncryptDecryptCheck(Func<string, string, string> checkEncryptDecryptPassword)
        {
            checkEncryptDecryptPassword(USERPASSWORD, MASTERPASSWORD);
            string encryptedPassword = checkEncryptDecryptPassword(USERPASSWORD, string.Empty);
            Assert.AreNotEqual(string.Empty, encryptedPassword, "Encrypted password by empty master password isn't valid");
            encryptedPassword = checkEncryptDecryptPassword(string.Empty, MASTERPASSWORD);
            Assert.AreEqual(string.Empty, encryptedPassword, "Encrypted empty user password isn't valid");
        }
    }
}
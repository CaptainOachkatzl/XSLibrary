﻿namespace XSLibrary.Cryptography.AccountManagement
{
    public class UserData
    {
        public string Username { get; private set; }
        public byte[] PasswordHash { get; private set; }
        public byte[] Salt { get; private set; }
        public int Difficulty { get; private set; }

        public UserData(string userName, byte[] passwordHash, byte[] salt, int difficulty)
        {
            Username = userName;
            PasswordHash = passwordHash;
            Salt = salt;
            Difficulty = difficulty;
        }
    }
}
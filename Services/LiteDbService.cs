using LiteDB;

namespace WhatToEatApp.Services
{
    public interface ILiteDbService
    {
        LiteDatabase CreateInstance();
    }

    public class LiteDbService : ILiteDbService
    {
        private readonly IConfiguration _configuration;

        public LiteDbService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public LiteDatabase CreateInstance()
        {
            return new LiteDatabase(_configuration.GetConnectionString("LiteDbConnection"));
        }
    }


}
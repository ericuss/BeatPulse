﻿using System;
using System.Collections.Generic;
using System.Net.Http;

namespace BeatPulse.Uris
{

    public interface IUriOptions
    {
        IUriOptions UseGet();
        IUriOptions UsePost();
        IUriOptions UseHttpMethod(HttpMethod methodToUse);
        IUriOptions ExpectHttpCode(int codeToExpect);
        IUriOptions ExpectHttpCodes(int minCodeToExpect, int maxCodeToExpect);
        IUriOptions AddCustomHeader(string name, string value);
    }

    public class UriOptions : IUriOptions
    {
        public HttpMethod HttpMethod { get; private set; }
        public (int Min, int Max)? ExpectedHttpCodes { get; private set; }
        public Uri Uri { get; }

        private readonly List<(string Name, string Value)> _headers;

        internal IEnumerable<(string Name, string Value)> Headers => _headers;

        public UriOptions(Uri uri)
        {
            Uri = uri;
            ExpectedHttpCodes = null;
            HttpMethod = null;
            _headers = new List<(string Name, string Value)>();
        }

        public IUriOptions AddCustomHeader(string name, string value)
        {
            _headers.Add((name, value));
            return this;
        }


        IUriOptions IUriOptions.UseGet()
        {
            HttpMethod = HttpMethod.Get;
            return this;
        }
        IUriOptions IUriOptions.UsePost()
        {
            HttpMethod = HttpMethod.Post;
            return this;
        }

        IUriOptions IUriOptions.ExpectHttpCode(int codeToExpect)
        {
            ExpectedHttpCodes = (codeToExpect, codeToExpect);
            return this;
        }

        IUriOptions IUriOptions.ExpectHttpCodes(int minCodeToExpect, int maxCodeToExpect)
        {
            ExpectedHttpCodes = (minCodeToExpect, maxCodeToExpect);
            return this;
        }
        IUriOptions IUriOptions.UseHttpMethod(HttpMethod methodToUse)
        {
            HttpMethod = methodToUse;
            return this;
        }
    }

    public class UriLivenessOptions
    {
        private readonly List<UriOptions> _uris;
        internal HttpMethod HttpMethod { get; private set; }
        internal IEnumerable<UriOptions> Uris => _uris;

        internal (int Min, int Max) ExpectedHttpCodes { get; private set; }

        public UriLivenessOptions()
        {
            _uris = new List<UriOptions>();
            ExpectedHttpCodes = (200, 299);              // HTTP Succesful status codes
            HttpMethod = HttpMethod.Get;
        }

        public UriLivenessOptions UseGet()
        {
            HttpMethod = HttpMethod.Get;
            return this;
        }

        public UriLivenessOptions UsePost()
        {
            HttpMethod = HttpMethod.Post;
            return this;
        }

        public UriLivenessOptions UseHttpMethod(HttpMethod methodToUse)
        {
            HttpMethod = methodToUse;
            return this;
        }

        public UriLivenessOptions AddUri(Uri uriToAdd, Action<IUriOptions> uriOptions = null)
        {
            var uri = new UriOptions(uriToAdd);
            uriOptions?.Invoke(uri);
            _uris.Add(uri);
            return this;
        }

        public UriLivenessOptions ExpectHttpCode(int codeToExpect)
        {
            ExpectedHttpCodes = (codeToExpect, codeToExpect);
            return this;
        }
        public UriLivenessOptions ExpectHttpCodes(int minCodeToExpect, int maxCodeToExpect)
        {
            ExpectedHttpCodes = (minCodeToExpect, maxCodeToExpect);
            return this;
        }


    }
}

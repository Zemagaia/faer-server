using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Web;
using Anna.Util;

namespace Anna.Request; 

public class Request {
    public Request(HttpListenerRequest request) {
        this.ListenerRequest = request;
        Headers = request.Headers.ToDictionary();
        QueryString = new ArgumentsDynamic(HttpUtility.ParseQueryString(request.Url.Query));
    }

    public HttpListenerRequest ListenerRequest { get; }

    public string HttpMethod => ListenerRequest.HttpMethod;

    public IDictionary<string, IEnumerable<string>> Headers { get; set; }

    public Stream InputStream => ListenerRequest.InputStream;

    public Encoding ContentEncoding => ListenerRequest.ContentEncoding;

    public string RawUrl => ListenerRequest.Url.ToString();

    public int ContentLength => int.Parse(Headers["Content-Length"].First());

    public Uri Url => ListenerRequest.Url;

    public dynamic UriArguments { get; private set; }

    public dynamic QueryString { get; }

    /// <summary>
    ///     Returns a single-element observable sequence of the request body (typically only present for POST and PUT requests)
    ///     as a string,
    ///     using the encoding specified in the HTTP request. Note: this method closes the <c>InputStream</c> can thus can only
    ///     be
    ///     called once per request. Also, if the Content-Length HTTP header is not correctly set, the wrong amount of data may
    ///     be read.
    /// </summary>
    /// <param name="maxContentLength">
    ///     The maximum amount of bytes that will be read, to avoid memory issues with large
    ///     uploads. Defaults to 50 kB. Use InputStream directly to read chunked data if you expect large uploads.
    /// </param>
    /// <returns>A single-element observable that contains the request body. Subscribe to it to asynchronously read the </returns>
    public IObservable<string> GetBody(int maxContentLength = 50000) {
        var bufferSize = Math.Min(maxContentLength, ContentLength);

        var reader = new StreamReader(InputStream, ContentEncoding);
        var buffer = new byte[bufferSize];

        return Observable.FromAsyncPattern<byte[], int, int, int>(InputStream.BeginRead, InputStream.EndRead)(buffer, 0,
                bufferSize)
            .Select(bytesRead => ContentEncoding.GetString(buffer, 0, bytesRead));
    }

    internal void LoadArguments(NameValueCollection nameValueCollection) {
        UriArguments = new ArgumentsDynamic(nameValueCollection);
    }
}
// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Net;
using System.Threading;
using UnityEngine;

namespace ARETT
{
	/// <summary>
	/// Wrapper of the HttpListener class which provides an easier to use HTTP server
	/// </summary>
	public class HTTPServer
	{
		private readonly HttpListener httpListener = new HttpListener();
		private readonly Func<HttpListenerRequest, string> responderMethod;

		public HTTPServer(string[] prefixes, Func<HttpListenerRequest, string> method)
		{
			if (!HttpListener.IsSupported)
				throw new NotSupportedException("HttpListener not supported!");

			// URI prefixes are required, for example 
			// "http://localhost:8080/index/"
			if (prefixes == null || prefixes.Length == 0)
				throw new ArgumentException("prefixes");

			// A responder method is required
			if (method == null)
				throw new ArgumentException("method");  

			foreach (string s in prefixes)
				httpListener.Prefixes.Add(s);

			responderMethod = method;
			httpListener.Start();
		}

		public HTTPServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
			: this(prefixes, method) { }

		public void Run()
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
				Debug.Log("[EyeTracking] Webserver running");
				try
				{
					while (httpListener.IsListening)
					{
						ThreadPool.QueueUserWorkItem((c) =>
						{
							var ctx = c as HttpListenerContext;
							try
							{
								string rstr = responderMethod(ctx.Request);
								byte[] buf = Encoding.UTF8.GetBytes(rstr);
								ctx.Response.ContentLength64 = buf.Length;
								ctx.Response.OutputStream.Write(buf, 0, buf.Length);
							}
							catch { } // suppress any exceptions
						finally
							{
							// always close the stream
							ctx.Response.OutputStream.Close();
							}
						}, httpListener.GetContext());
					}
				}
				catch { } // suppress any exceptions
		});
		}

		public void Stop()
		{
			httpListener.Stop();
			httpListener.Close();
		}
	}
}

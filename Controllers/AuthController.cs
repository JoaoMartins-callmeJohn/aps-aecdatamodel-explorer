﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly ILogger<AuthController> _logger;
	private readonly ForgeService _forgeService;

	public AuthController(ILogger<AuthController> logger, ForgeService forgeService)
	{
		_logger = logger;
		_forgeService = forgeService;
	}

	public static async Task<Tokens> PrepareTokens(HttpRequest request, HttpResponse response, ForgeService forgeService)
	{
		if (!request.Cookies.ContainsKey("internal_token"))
		{
			return null;
		}
		var tokens = new Tokens
		{
			PublicToken = request.Cookies["public_token"],
			InternalToken = request.Cookies["internal_token"],
			RefreshToken = request.Cookies["refresh_token"],
			ExpiresAt = DateTime.Parse(request.Cookies["expires_at"])
		};
		if (tokens.ExpiresAt < DateTime.Now.ToUniversalTime())
		{
			tokens = await forgeService.RefreshTokens(tokens);
			response.Cookies.Append("public_token", tokens.PublicToken);
			response.Cookies.Append("internal_token", tokens.InternalToken);
			response.Cookies.Append("refresh_token", tokens.RefreshToken);
			response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
		}
		return tokens;
	}

	[HttpGet("login")]
	public ActionResult Login()
	{
		var redirectUri = _forgeService.GetAuthorizationURL();
		return Redirect(redirectUri);
	}

	[HttpGet("logout")]
	public ActionResult Logout()
	{
		Response.Cookies.Delete("public_token");
		Response.Cookies.Delete("internal_token");
		Response.Cookies.Delete("refresh_token");
		Response.Cookies.Delete("expires_at");
		return Redirect("/");
	}

	[HttpGet("callback")]
	public async Task<ActionResult> Callback(string code)
	{
		var tokens = await _forgeService.GenerateTokens(code);
		Response.Cookies.Append("public_token", tokens.PublicToken);
		Response.Cookies.Append("internal_token", tokens.InternalToken);
		Response.Cookies.Append("refresh_token", tokens.RefreshToken);
		Response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
		return Redirect("/");
	}

	[HttpGet("profile")]
	public async Task<dynamic> GetProfile()
	{
		var tokens = await PrepareTokens(Request, Response, _forgeService);
		if (tokens == null)
		{
			return Unauthorized();
		}
		dynamic profile = await _forgeService.GetUserProfile(tokens);
		return new
		{
			name = string.Format("{0} {1}", profile.firstName, profile.lastName)
		};
	}

	[HttpGet("token")]
	public async Task<dynamic> GetPublicToken()
	{
		var tokens = await PrepareTokens(Request, Response, _forgeService);
		if (tokens == null)
		{
			return Unauthorized();
		}
		return new
		{
			access_token = tokens.PublicToken,
			token_type = "Bearer",
			expires_in = Math.Floor((tokens.ExpiresAt - DateTime.Now.ToUniversalTime()).TotalSeconds)
		};
	}
}
using System;
using RailFactory.Frontend.Security;

namespace RailFactory.Frontend.Tests;

public class LocationRewriteTests
{
    [Fact]
    public void RewriteLocationIfNeeded_RelativeLocation_RewritesToPublicOrigin()
    {
        var rewritten = RedirectLocationRewriter.RewriteLocationIfNeeded(
            requestScheme: "https",
            publicHost: "apparent-driving-horse.ngrok-free.app",
            gatewayHost: "identity-access-management",
            location: new Uri("/auth/callback?code=abc&state=def", UriKind.Relative));

        Assert.Equal("https://apparent-driving-horse.ngrok-free.app/auth/callback?code=abc&state=def", rewritten);
    }

    [Fact]
    public void RewriteLocationIfNeeded_AbsoluteLocationGatewayHost_RewritesToPublicOrigin()
    {
        var rewritten = RedirectLocationRewriter.RewriteLocationIfNeeded(
            requestScheme: "https",
            publicHost: "apparent-driving-horse.ngrok-free.app",
            gatewayHost: "gateway",
            location: new Uri("http://gateway:5080/auth/callback?code=abc&state=def"));

        Assert.Equal("https://apparent-driving-horse.ngrok-free.app/auth/callback?code=abc&state=def", rewritten);
    }

    [Fact]
    public void RewriteLocationIfNeeded_AbsoluteLocationExternalHost_PreservesRedirect()
    {
        var rewritten = RedirectLocationRewriter.RewriteLocationIfNeeded(
            requestScheme: "https",
            publicHost: "apparent-driving-horse.ngrok-free.app",
            gatewayHost: "gateway",
            location: new Uri("https://accounts.google.com/somewhere"));

        Assert.Null(rewritten);
    }

    [Fact]
    public void RewriteLocationIfNeeded_AbsoluteLocationPublicHost_PreservesRedirect()
    {
        var rewritten = RedirectLocationRewriter.RewriteLocationIfNeeded(
            requestScheme: "https",
            publicHost: "apparent-driving-horse.ngrok-free.app",
            gatewayHost: "gateway",
            location: new Uri("https://apparent-driving-horse.ngrok-free.app/auth/callback?x=1"));

        Assert.Null(rewritten);
    }
}

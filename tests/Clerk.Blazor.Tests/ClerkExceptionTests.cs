namespace Clerk.Blazor.Tests;

public class ClerkExceptionTests
{
    [Test]
    public void NormalizedField_FallsBackToMatchingMessage()
    {
        var ex = new ClerkException("form_errors", "Request failed",
            [new("invalid", "Invalid email", null, "email_address")]);
        Assert.That(ex.FieldError("emailAddress"), Is.EqualTo("Invalid email"));
    }

    [Test]
    public void InteropError_MapsToTypedExceptions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new InteropError { IsReverification = true, Message = "m" }.ToException(), Is.TypeOf<ClerkReverificationRequiredException>());
            Assert.That(new InteropError { Code = "clerk_offline", Message = "m" }.ToException(), Is.TypeOf<ClerkOfflineException>());

            var ex = new InteropError { Code = "form_password_incorrect", Message = "bad", Status = 422, ClerkTraceId = "t1" }.ToException();
            Assert.That(ex, Is.TypeOf<ClerkException>());
            Assert.That(ex.Code, Is.EqualTo("form_password_incorrect"));
            Assert.That(ex.Status, Is.EqualTo(422));
            Assert.That(ex.TraceId, Is.EqualTo("t1"));
        });
    }

    [Test]
    public void FieldAndGlobalMessages()
    {
        var ex = new ClerkException("form_errors", "Request failed",
        [
            new("form_password_incorrect", "Incorrect", "Password is incorrect.", "password"),
            new("form_identifier_not_found", "Not found", "Couldn't find your account.", "identifier"),
            new("too_many_requests", "Slow down", "Too many requests.", null),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(ex.FieldError("password"), Is.EqualTo("Password is incorrect."));
            Assert.That(ex.FieldError("Identifier"), Is.EqualTo("Couldn't find your account."));
            Assert.That(ex.FieldError("email_address"), Is.Null);
            Assert.That(ex.GlobalMessage, Is.EqualTo("Too many requests."));
            Assert.That(new ClerkException("x", "plain").GlobalMessage, Is.EqualTo("plain"));
        });
    }
}

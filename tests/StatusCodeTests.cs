namespace Simulation.Tests
{
    public class StatusCodeTests
    {
        [Test]
        public void VerifySuccessCode()
        {
            StatusCode a = StatusCode.Success(0);
            StatusCode b = StatusCode.Success(1);
            Assert.That(a.IsSuccess, Is.True);
            Assert.That(b.IsSuccess, Is.True);
            Assert.That(a.Code, Is.EqualTo(0));
            Assert.That(b.Code, Is.EqualTo(1));
        }

        [Test]
        public void VerifyFailure()
        {
            StatusCode a = StatusCode.Failure(0);
            StatusCode b = StatusCode.Failure(1);
            Assert.That(a.IsSuccess, Is.False);
            Assert.That(b.IsSuccess, Is.False);
            Assert.That(a.Code, Is.EqualTo(0));
            Assert.That(b.Code, Is.EqualTo(1));
        }

        [Test]
        public void MaxCodes()
        {
            StatusCode a = StatusCode.Success(byte.MaxValue);
            Assert.That(a.Code, Is.EqualTo(byte.MaxValue));
            Assert.That(a.IsSuccess, Is.True);

            StatusCode b = StatusCode.Failure(byte.MaxValue);
            Assert.That(b.Code, Is.EqualTo(byte.MaxValue));
            Assert.That(b.IsSuccess, Is.False);
        }
    }
}
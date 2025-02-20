using System;

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
            StatusCode a = StatusCode.Success(StatusCode.MaxCode);
            Assert.That(a.Code, Is.EqualTo(StatusCode.MaxCode));
            Assert.That(a.IsSuccess, Is.True);

            StatusCode b = StatusCode.Failure(StatusCode.MaxCode);
            Assert.That(b.Code, Is.EqualTo(StatusCode.MaxCode));
            Assert.That(b.IsSuccess, Is.False);
        }

        [Test]
        public void DefaultNotTheSame()
        {
            StatusCode a = default;
            StatusCode b = StatusCode.Continue;
            StatusCode c = StatusCode.Success(0);
            StatusCode d = StatusCode.Failure(0);

            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a, Is.Not.EqualTo(d));
            Assert.That(b, Is.Not.EqualTo(c));
            Assert.That(b, Is.Not.EqualTo(d));

            Assert.That(StatusCode.Termination, Is.Not.EqualTo(a));
            Assert.That(StatusCode.Termination, Is.Not.EqualTo(b));
            Assert.That(StatusCode.Termination, Is.Not.EqualTo(c));
            Assert.That(StatusCode.Termination, Is.Not.EqualTo(d));
        }

#if DEBUG
        [Test]
        public void ThrowIfOutOfRange()
        {
            StatusCode a = StatusCode.Success(StatusCode.MaxCode);
            Assert.Throws<ArgumentOutOfRangeException>(() => StatusCode.Success(StatusCode.MaxCode + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StatusCode.Failure(StatusCode.MaxCode + 1));
        }
#endif
    }
}
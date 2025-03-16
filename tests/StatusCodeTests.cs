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
            StatusCode defaultCode = default;
            StatusCode continueCode = StatusCode.Continue;
            StatusCode success = StatusCode.Success(0);
            StatusCode failure = StatusCode.Failure(0);

            Assert.That(defaultCode, Is.Not.EqualTo(continueCode));
            Assert.That(continueCode, Is.Not.EqualTo(defaultCode));
            Assert.That(defaultCode, Is.Not.EqualTo(success));
            Assert.That(defaultCode, Is.Not.EqualTo(failure));
            Assert.That(continueCode, Is.Not.EqualTo(success));
            Assert.That(continueCode, Is.Not.EqualTo(failure));

            Assert.That(StatusCode.Termination, Is.Not.EqualTo(defaultCode));
            Assert.That(StatusCode.Termination, Is.Not.EqualTo(continueCode));
            Assert.That(StatusCode.Termination, Is.Not.EqualTo(success));
            Assert.That(StatusCode.Termination, Is.Not.EqualTo(failure));
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
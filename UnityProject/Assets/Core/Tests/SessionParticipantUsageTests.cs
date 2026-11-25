using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core.Tests
{
    // Counting runs only in the Editor and development builds; EditMode tests always run in the Editor.
    public class SessionParticipantUsageTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            foreach (var created in _created)
            {
                if (created != null) Object.DestroyImmediate(created);
            }
            _created.Clear();
        }

        [Test]
        public void NothingTracked_HoldsNothing()
        {
            var usage = (ISessionParticipantUsage)new TrackingParticipant();

            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void TrackedViews_AreCounted()
        {
            var participant = new TrackingParticipant();

            participant.ShowView(NewView());
            participant.ShowView(NewView());

            Assert.That(((ISessionParticipantUsage)participant).HeldViews, Is.EqualTo(2));
        }

        [Test]
        public void ReleasedView_IsNoLongerCounted()
        {
            var participant = new TrackingParticipant();
            var held = participant.ShowView(NewView());

            held.Dispose();

            Assert.That(((ISessionParticipantUsage)participant).HeldViews, Is.EqualTo(0));
        }

        [Test]
        public void DestroyedView_IsNoLongerCounted()
        {
            var participant = new TrackingParticipant();
            var view = NewView();
            participant.ShowView(view);
            participant.ShowView(NewView());

            Object.DestroyImmediate(view);

            Assert.That(((ISessionParticipantUsage)participant).HeldViews, Is.EqualTo(1));
        }

        [Test]
        public void TrackingNoView_Fails()
        {
            var participant = new TrackingParticipant();

            Assert.Throws<ArgumentNullException>(() => participant.ShowView(null));
            Assert.That(((ISessionParticipantUsage)participant).HeldViews, Is.EqualTo(0));
        }

        [Test]
        public void TrackedJobs_AreCounted()
        {
            var participant = new TrackingParticipant();

            participant.StartJob();
            participant.StartJob();

            Assert.That(((ISessionParticipantUsage)participant).RunningJobs, Is.EqualTo(2));
        }

        [Test]
        public void FinishedJob_IsNoLongerCounted()
        {
            var participant = new TrackingParticipant();
            var job = participant.StartJob();

            job.Dispose();

            Assert.That(((ISessionParticipantUsage)participant).RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void FinishingAJobTwice_CountsItOnce()
        {
            var participant = new TrackingParticipant();
            var job = participant.StartJob();
            participant.StartJob();

            job.Dispose();
            job.Dispose();

            Assert.That(((ISessionParticipantUsage)participant).RunningJobs, Is.EqualTo(1));
        }

        [Test]
        public void ViewsAndJobs_AreCountedApart()
        {
            var participant = new TrackingParticipant();
            participant.ShowView(NewView());
            participant.StartJob().Dispose();

            var usage = (ISessionParticipantUsage)participant;

            Assert.That(usage.HeldViews, Is.EqualTo(1));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));
        }

        private GameObject NewView()
        {
            var view = new GameObject("tracked-view");
            _created.Add(view);
            return view;
        }

        private sealed class TrackingParticipant : SessionParticipantBase
        {
            public IDisposable ShowView(Object view) => TrackView(view);

            public IDisposable StartJob() => TrackJob();

            protected override void OnRun() { }
        }
    }
}

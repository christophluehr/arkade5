﻿using System.Collections.Generic;
using Arkivverket.Arkade.Core.Noark5;
using Arkivverket.Arkade.Resources;

namespace Arkivverket.Arkade.Tests.Noark5
{
    /// <summary>
    ///     Noark5 - test #28
    /// </summary>
    public class NumberOfCrossReferences : Noark5XmlReaderBaseTest
    {
        private int _classReferenceCount;
        private int _folderReferenceCount;

        public override string GetName()
        {
            return Noark5Messages.NumberOfCrossReferences;
        }

        public override TestType GetTestType()
        {
            return TestType.ContentAnalysis;
        }

        protected override List<TestResult> GetTestResults()
        {
            return new List<TestResult>
            {
                new TestResult(ResultType.Success, new Location(""), "Referanser til klasse: " + _classReferenceCount),
                new TestResult(ResultType.Success, new Location(""), "Referanser til mappe: " + _folderReferenceCount)
            };
        }

        protected override void ReadStartElementEvent(object sender, ReadElementEventArgs eventArgs)
        {
            if (eventArgs.NameEquals("referanseTilKlasse"))
                _classReferenceCount++;

            if (eventArgs.NameEquals("referanseTilMappe"))
                _folderReferenceCount++;
        }

        protected override void ReadAttributeEvent(object sender, ReadElementEventArgs eventArgs)
        {
        }

        protected override void ReadEndElementEvent(object sender, ReadElementEventArgs eventArgs)
        {
        }

        protected override void ReadElementValueEvent(object sender, ReadElementEventArgs eventArgs)
        {
        }
    }
}

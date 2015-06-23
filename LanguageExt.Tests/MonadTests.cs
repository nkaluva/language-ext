﻿using NUnit.Framework;
using LanguageExt;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using System.Collections.Immutable;
using System.Collections.Generic;
using System;

namespace LanguageExtTests
{
    [TestFixture]
    public class MonadTests
    {
        [Test]
        public void WriterTest()
        {
            var logNumber = fun((int x) => Writer(x, List("Got number: " + x)));

            var multWithLog = from a in logNumber(3)
                              from b in logNumber(5)
                              from _ in tell("Gonna multiply these two")
                              select a * b;

            var res = multWithLog();

            Assert.IsTrue(length(res.Output) == 3);
            Assert.IsTrue(res.Value == 15);
            Assert.IsTrue(head(res.Output) == "Got number: 3");
            Assert.IsTrue(head(tail(res.Output)) == "Got number: 5");
            Assert.IsTrue(head(tail(tail(res.Output))) == "Gonna multiply these two");
        }

        private class Bindings
        {
            public readonly Map<string, int> Map;
            public Bindings(params Tuple<string, int>[] items)
            {
                Map = Map(items);
            }

            public static Bindings New(params Tuple<string, int>[] items)
            {
                return new Bindings(items);
            }
        }

        [Test]
        public void ReaderAskTest()
        {
            var lookupVar = fun((string name, Bindings bindings) => LanguageExt.Map.find(bindings.Map, name).IfNone(0));

            var calcIsCountCorrect = from   count    in asks((Bindings env) => lookupVar("count", env))
                                     from   bindings in ask<Bindings>()
                                     select count == LanguageExt.Map.length(bindings.Map);

            var sampleBindings = Bindings.New(
                                    Tuple("count", 3), 
                                    Tuple("1", 1), 
                                    Tuple("b", 2)
                                    );

            bool res = calcIsCountCorrect(sampleBindings).Value;

            Assert.IsTrue(res);
        }

        [Test]
        public void ReaderLocalTest()
        {
            var calculateContentLen = from content in ask<string>()
                                      select content.Length;

            var calculateModifiedContentLen = local(content => "Prefix " + content, calculateContentLen);

            var s = "12345";
            var modifiedLen = calculateModifiedContentLen(s);
            var len = calculateContentLen(s);

            Assert.IsTrue(modifiedLen.Value == 12);
            Assert.IsTrue(len.Value == 5);
        }

        [Test]
        public void ReaderBottomTest()
        {
            var v1 = Reader<int, int>(10);
            var v2 = Reader<int, int>(10);

            var rdr = from x in v1
                      from y in v2
                      from c in ask<int>()
                      where x * c > 50 && y * c > 50
                      select (x + y) * c;

            Assert.IsTrue(rdr(10) == 200);
            Assert.IsTrue(rdr(2) == 0);
            Assert.IsTrue(rdr(2).IsBottom);
        }

        [Test]
        public void StateTest()
        {
            var comp = from inp in get<string>()
                       let  hw  =  inp + ", world"
                       from _   in put(hw)
                       select hw.Length;

            var r = comp("hello");

            Assert.IsTrue(r.Value == 12);
            Assert.IsTrue(r.State == "hello, world");
        }

        [Test]
        public void StateBottomTest()
        {
            var v1 = State<int, int>(10);
            var v2 = State<int, int>(10);

            var rdr = from x in v1
                      from y in v2
                      from c in get<int>()
                      where x * c > 50 && y * c > 50
                      select (x + y) * c;

            Assert.IsTrue(rdr(10) == 200);
            Assert.IsTrue(rdr(2) == 0);
            Assert.IsTrue(rdr(2).IsBottom);
        }

    }
}

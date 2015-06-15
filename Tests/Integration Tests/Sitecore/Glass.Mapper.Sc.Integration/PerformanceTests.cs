/*
   Copyright 2012 Michael Edwards
 
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 
*/ 
//-CRE-


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Glass.Mapper.IoC;
using Glass.Mapper.Pipelines.ObjectConstruction.Tasks.CreateConcrete;
using Glass.Mapper.Sc.Configuration;
using Glass.Mapper.Sc.Configuration.Attributes;
using NUnit.Framework;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Glass.Mapper.Sc.Integration
{
    [TestFixture]
    
    public class PerformanceTests
    {

        private string _expected;
        private Guid _id;
        private Context _context;
        private Database _db;
        private SitecoreService _service;
        private bool _hasRun = false;
        private Stopwatch _glassWatch;
        private Stopwatch _rawWatch;
        private double _glassTotal;
        private double _rawTotal;

        [SetUp]
        public void Setup()
        {
            if (_hasRun)
            {
                return;
            }
            else
                _hasRun = true;

            _glassWatch = new Stopwatch();
            _rawWatch= new Stopwatch();
            

            _expected = "hello world";
            _id = new Guid("{59784F74-F830-4BCD-B1F0-1A08616EF726}");

            _context = Context.Create(Utilities.CreateStandardResolver());


            _context.Load(new SitecoreAttributeConfigurationLoader("Glass.Mapper.Sc.Integration"));

            _db = Factory.GetDatabase("master");

            //       service.Profiler = new SimpleProfiler();

            _service = new SitecoreService(_db);

            var item = _db.GetItem(new ID(_id));

            using (new ItemEditing(item, true))
            {
                item["Field"] = _expected;
            }

            _service.Cast<StubClassWithLotsOfProperties>(item);
            _service.Cast<StubClass>(item);
            const string path = "/sitecore/content/Tests/PerformanceTests/InheritanceTest";
            var inheritanceItem = _db.GetItem(path);


            _service.Cast<StubClassLevel5>(inheritanceItem);
            _service.Cast<StubClassLevel1>(inheritanceItem);
        }

       

        [Test]
        [Timeout(120000)]
        [Repeat(10000)]
        public void GetItems(
            [Values(1, 1000, 10000, 50000)] int count
            )
        {

            for (int i = 0; i < count; i++)
            {
                _glassWatch.Reset();
                _rawWatch.Reset();

                _rawWatch.Start();
                var rawItem = _db.GetItem(new ID(_id));
                var value1 = rawItem["Field"];
                _rawWatch.Stop();
                _rawTotal = _rawWatch.ElapsedTicks;

                _glassWatch.Start();
                var glassItem = _service.GetItem<StubClass>(_id);
                var value2 = glassItem.Field;
                _glassWatch.Stop();
                _glassTotal = _glassWatch.ElapsedTicks;

            }

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance Test Count: {0} Ratio: {1} Average: {2}".Formatted(count, total, _glassTotal/count));
        }

        [Test]
        [Timeout(120000)]
        [Repeat(10000)]
        public void CastItems(
            [Values(1, 1000, 10000, 50000)] int count
            )
        {
            var rawItem = _db.GetItem(new ID(_id));
            _glassWatch.Reset();
            _rawWatch.Reset();

            _rawWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var stringIWant = rawItem["Field"];
            }
            _rawWatch.Stop();
            _rawTotal = _rawWatch.ElapsedTicks;

            _glassWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var stringIWant = _service.Cast<StubClass>(rawItem).Field;
            }
            _glassWatch.Stop();
            _glassTotal = _glassWatch.ElapsedTicks;

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance Test Count: {0} Ratio: {1} Average: {2}".Formatted(count, total, _glassTotal / count));
        }

        [Test]
        [Timeout(120000)]
        [Repeat(10000)]
        public void GlassCastItems(
            [Values(1, 1000, 10000, 50000)] int count
            )
        {
            var rawItem = _db.GetItem(new ID(_id));
            _glassWatch.Reset();
            _rawWatch.Reset();

            _rawWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var stringIWant = rawItem["Field"];
            }
            _rawWatch.Stop();
            _rawTotal = _rawWatch.ElapsedTicks;

            _glassWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var stringIWant = rawItem.GlassCast<StubClass>().Field;
            }
            _glassWatch.Stop();
            _glassTotal = _glassWatch.ElapsedTicks;

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance Test Count: {0} Ratio: {1} Average: {2}".Formatted(count, total, _glassTotal / count));
        }

        [Test]
        [Timeout(120000)]
        [Repeat(10000)]
        public void ExpressionBuildIsolationTest(
            [Values(1, 1000, 10000, 50000)] int count
            )
        {

            Stopwatch thirdWatch = new Stopwatch();

            var rawItem = _db.GetItem(new ID(_id));
            _glassWatch.Reset();
            _rawWatch.Reset();

            _rawWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var stringIWant = rawItem["Field"];
            }
            _rawWatch.Stop();
            _rawTotal = _rawWatch.ElapsedTicks;

            var tempResult = _service.CreateType<StubClass>(rawItem);
            var config = _service.GlassContext.TypeConfigurations[typeof (StubClass)];
            var mappingContext = new SitecoreDataMappingContext(null, rawItem, _service);
            var activator = config.DefaultConstructorActivator;

            _glassWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var result = (StubClass) activator(mappingContext);
                var stringIWant = result.Field;
            }
            _glassWatch.Stop();


            thirdWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var glassItem = _service.GetItem<StubClass>(_id);
                var value2 = glassItem.Field;
            }
            thirdWatch.Stop();



            _glassTotal = _glassWatch.ElapsedTicks;

            double total = _glassTotal / _rawTotal;

            Console.WriteLine("Performance Test Count: {0} Ratio: {1} Average: {2}, Third Ratio: {3} Average {4}".Formatted(count, total, _glassTotal / count, thirdWatch.ElapsedTicks / _rawTotal, thirdWatch.ElapsedTicks / count));
        }

        [Test]
        [Timeout(120000)]
        [Repeat(10000)]
        public void GlassCastItemsWithService(
            [Values(1, 1000, 10000, 50000)] int count
            )
        {
            var rawItem = _db.GetItem(new ID(_id));
            _glassWatch.Reset();
            _rawWatch.Reset();

            _rawWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var stringIWant = rawItem["Field"];
            }
            _rawWatch.Stop();
            _rawTotal = _rawWatch.ElapsedTicks;

            _glassWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var result = _service.CreateType<StubClass>(rawItem);
                var stringIWant = result.Field;
            }
            _glassWatch.Stop();
            _glassTotal = _glassWatch.ElapsedTicks;

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance Test Count: {0} Ratio: {1} Average: {2}".Formatted(count, total, _glassTotal / count));
        }

        [Test]
        [Timeout(120000)]
        [Repeat(10000)]
        public void GetItems_LotsOfProperties(
            [Values(1000, 10000, 50000)] int count
            )
        {
            var warmUp = _service.GetItem<StubClassWithLotsOfProperties>(_id);


            _glassWatch.Reset();
            _rawWatch.Reset();

            _rawWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var rawItem = _db.GetItem(new ID(_id));
                var value1 = rawItem["Field"];
            }
            _rawWatch.Stop();
            _rawTotal = _rawWatch.ElapsedTicks;

            for (int i = 0; i < count; i++)
            {
                _glassWatch.Start();
                var glassItem = _service.GetItem<StubClassWithLotsOfProperties>(_id);
                var value2 = glassItem.Field1;
                _glassWatch.Stop();
            }
            _glassTotal = _glassWatch.ElapsedTicks;

            double total = _glassTotal/_rawTotal;
            Console.WriteLine("Performance Test Count: {0} Ratio: {1} Average: {2}".Formatted(count, total,
                _glassTotal/count));
        }

        [Test]
        [Timeout(120000)]
        [Repeat(10000)]
        public void GetItems_LotsOfProperties_NotRaw(
            [Values(1000, 10000, 50000)] int count
            )
        {

            for (int i = 0; i < count; i++)
            {
                _glassWatch.Reset();
                _rawWatch.Reset();

                _rawWatch.Start();
                var rawItem = _db.GetItem(new ID(_id));
                var value1 = rawItem["Field"];
                _rawWatch.Stop();
                _rawTotal = _rawWatch.ElapsedTicks;

                _glassWatch.Start();
                var glassItem = _service.GetItem<StubClassWithLotsOfPropertiesNotRaw>(_id);
                var value2 = glassItem.Field1;
                _glassWatch.Stop();
                _glassTotal = _glassWatch.ElapsedTicks;

            }

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance Test Count: {0} Ratio: {1} Average: {2}".Formatted(count, total, _glassTotal / count));
        }

        [Test]
        [Timeout(120000)]
        public void GetItems_InheritanceTest(
            [Values(100, 200, 300)] int count
            )
        {
            string path = "/sitecore/content/Tests/PerformanceTests/InheritanceTest";

            for (int i = 0; i < count; i++)
            {
                _glassWatch.Reset();
                _rawWatch.Reset();

                _rawWatch.Start();

                var glassItem1 = _service.GetItem<StubClassLevel5>(path);
                var value1 = glassItem1.Field;

                _rawWatch.Stop();
                _rawTotal = _rawWatch.ElapsedTicks;

                _glassWatch.Start();
                var glassItem2 = _service.GetItem<StubClassLevel1>(path);
                var value2 = glassItem2.Field;
                _glassWatch.Stop();
                _glassTotal = _glassWatch.ElapsedTicks;

            }

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance inheritance Test Count: {0},  Single: {1}, 5 Levels: {2}, Ratio: {3}".Formatted(count, _rawTotal, _glassTotal, total));
        }

        [Test]
        [Timeout(120000)]
        public void CastWithService_NoPropertyVsProperty(
            [Values(3000)] int count
            )
        {
            string path = "/sitecore/content/Tests/PerformanceTests/InheritanceTest";
            var item = _db.GetItem(path);

            var glassItemWarm = item.GlassCast<StubNoProperties>(_service);
            var glassItemWarm1 = item.GlassCast<StubOneProperty>(_service);
            

            for (int i = 0; i < count; i++)
            {
                _glassWatch.Reset();
                _rawWatch.Reset();

                _rawWatch.Start();

                var glassItem1 = item.GlassCast<StubNoProperties>(_service);

                _rawWatch.Stop();
                _rawTotal += _rawWatch.ElapsedTicks;

                _glassWatch.Start();
                var glassItem2 = item.GlassCast<StubOneProperty>(_service);
                _glassWatch.Stop();
                _glassTotal += _glassWatch.ElapsedTicks;

            }

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance inheritance Test Count: {0},  No Prop: {1},  Gls: {2}, Ratio: {3}".Formatted(count, _rawTotal, _glassTotal, total));
        }

        [Test]
        [Timeout(120000)]
        public void CastWithService_ItemVsProperty(
            [Values( 3000)] int count
            )
        {
            string path = "/sitecore/content/Tests/PerformanceTests/InheritanceTest";
            var item = _db.GetItem(path);

            var glassItemWarm = item.GlassCast<StubNoProperties>(_service);
            var glassItemWarm1 = item.GlassCast<StubOneProperty>(_service);


            for (int i = 0; i < count; i++)
            {
                _glassWatch.Reset();
                _rawWatch.Reset();

                _rawWatch.Start();

                var value1 = item["Field"];

                _rawWatch.Stop();
                _rawTotal += _rawWatch.ElapsedTicks;

                _glassWatch.Start();
                var glassItem2 = item.GlassCast<StubOneProperty>(_service);

                _glassWatch.Stop();
                _glassTotal += _glassWatch.ElapsedTicks;

            }

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance inheritance Test Count: {0},  Sc: {1},  Gls: {2}, Ratio: {3}".Formatted(count, _rawTotal, _glassTotal, total));
        }

        [Test]
        [Timeout(120000)]
        public void CastWithService_ManualVsProperty(
            [Values(3000)] int count
            )
        {
            string path = "/sitecore/content/Tests/PerformanceTests/InheritanceTest";
            var item = _db.GetItem(path);

            var glassItemWarm = item.GlassCast<StubNoProperties>(_service);
            var glassItemWarm1 = item.GlassCast<StubOneProperty>(_service);


            for (int i = 0; i < count; i++)
            {
                _glassWatch.Reset();
                _rawWatch.Reset();

                _rawWatch.Start();

                var cls = new StubOneProperty();
                cls.Field = item["Field"];

                _rawWatch.Stop();
                _rawTotal += _rawWatch.ElapsedTicks;

                _glassWatch.Start();
                var glassItem2 = item.GlassCast<StubOneProperty>(_service);

                _glassWatch.Stop();
                _glassTotal += _glassWatch.ElapsedTicks;

            }

            double total = _glassTotal / _rawTotal;
            Console.WriteLine("Performance inheritance Test Count: {0},  Manual: {1},  Gls: {2}, Ratio: {3}".Formatted(count, _rawTotal, _glassTotal, total));
        }
        #region Stubs

        public class StubOneProperty
        {
            public virtual string Field { get; set; }
        }
        public class StubNoProperties
        {
            
        }

        [SitecoreType]
        public class StubClassWithLotsOfProperties
        {
            public virtual string Url { get; set; }

            [SitecoreField("Field",Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field1 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field2 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field3 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field4 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field5 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field6 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field7 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field8 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field9 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field10 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field11 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field12 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field13 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field14 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field15 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field16 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field17 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field18 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field19 { get; set; }

            [SitecoreField("Field", Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field20 { get; set; }


            [SitecoreId]
            public virtual Guid Id { get; set; }
        }

        [SitecoreType]
        public class StubClassWithLotsOfPropertiesNotRaw
        {
            [SitecoreField("Field")]
            public virtual string Field1 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field2 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field3 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field4 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field5 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field6 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field7 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field8 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field9 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field10 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field11 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field12 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field13 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field14 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field15 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field16 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field17 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field18 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field19 { get; set; }

            [SitecoreField("Field")]
            public virtual string Field20 { get; set; }


            [SitecoreId]
            public virtual Guid Id { get; set; }
        }


        [SitecoreType]
        public class StubClass
        {
            [SitecoreField(Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field { get; set; }

            [SitecoreId]
            public virtual Guid Id { get; set; }
        }

        [SitecoreType]
        public class StubClassLevel1 : StubClassLevel2
        {
            
        }
        [SitecoreType]
        public class StubClassLevel2 : StubClassLevel3
        {

        }
        [SitecoreType]
        public class StubClassLevel3 : StubClassLevel4
        {

        }
        [SitecoreType]
        public class StubClassLevel4 : StubClassLevel5
        {

        }
        [SitecoreType]
        public class StubClassLevel5
        {
            [SitecoreField(Setting = SitecoreFieldSettings.RichTextRaw)]
            public virtual string Field { get; set; }

            [SitecoreId]
            public virtual Guid Id { get; set; }
        }

        #endregion




        //   [Test]
        //   [Timeout(120000)]
        //   public void GetItems()
        //   {

        //       //Assign
        //       int[] counts = new int[] {1, 100, 1000, 10000, 50000, 100000, 150000,200000};
        //       foreach (var count in counts)
        //       {
        //           GetItemsTest(count);
        //       }
        //   }
        //   private void GetItemsTest(int count){

        //   var expected = "hello world";
        //       var id = new Guid("{59784F74-F830-4BCD-B1F0-1A08616EF726}");

        //       var context = Context.Create(new SitecoreConfig());


        //       context.Load(new SitecoreAttributeConfigurationLoader("Glass.Mapper.Sc.Integration"));

        //       var db = Sitecore.Configuration.Factory.GetDatabase("master");
        //       var service = new SitecoreService(db);

        ////       service.Profiler = new SimpleProfiler();

        //       var item = db.GetItem(new ID(id));
        //       using (new ItemEditing(item, true))
        //       {
        //           item["Field"] = expected;
        //       }

        //       //Act

        //       //get Sitecore raw
        //       var rawTotal = (long)0;
        //           var watch1 = new Stopwatch();

        //       for (int i = 0; i < count; i++)
        //       {

        //           watch1.Start();
        //           var rawItem = db.GetItem(new ID(id));
        //           var value = rawItem["Field"];
        //           watch1.Stop();
        //         Assert.AreEqual(expected, value);
        //           rawTotal += watch1.ElapsedTicks;
        //       }

        //       long rawAverage = rawTotal / count;

        //       //Console.WriteLine("Performance Test - 1000 - Raw - {0}", rawAverage);
        //      // Console.WriteLine("Raw ElapsedTicks to sec:  {0}", rawAverage / (double)Stopwatch.Frequency);

        //       var glassTotal = (long)0;
        //           var watch2 = new Stopwatch();
        //           for (int i = 0; i < count; i++)
        //       {

        //           watch2.Start();
        //           var glassItem = service.GetItem<StubClass>(id);
        //          var value = glassItem.Field;
        //           watch2.Stop();
        //           Assert.AreEqual(expected, value);
        //           glassTotal += watch2.ElapsedTicks;
        //       }


        //           long glassAverage = glassTotal / count;

        //      // Console.WriteLine("Performance Test - 1000 - Glass - {0}", glassAverage);
        //       //Console.WriteLine("Glass ElapsedTicks to sec:  {0}", glassAverage / (double)Stopwatch.Frequency);
        //       Console.WriteLine("{1}: Raw/Glass {0}", (double) glassAverage/(double)rawAverage, count);


        //       //Assert
        //       //ME - at the moment I am allowing glass to take twice the time. I would hope to reduce this
        //       //Assert.LessOrEqual(glassAverage, rawAverage*2);


        //   }

        [Test]
        public void DifferentActivation()
        {
            Type stubClassType = typeof(StubClassWithLotsOfProperties);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < 100000; i++)
            {
                var result = Activator.CreateInstance(stubClassType);
            }
            sw.Stop();

            Console.WriteLine("Activator.CreateInstance: {0}", sw.ElapsedTicks);

            sw.Restart();
            for (var i = 0; i < 100000; i++)
            {
                ActivationManager.CompiledActivator<object> activator = ActivationManager.GetActivator(stubClassType);
                var result = activator();
            }

            sw.Stop();

            Console.WriteLine("Compiled lambda: {0}", sw.ElapsedTicks);
        }

        [Test]
        public void CreateObject()
        {
            Type stubClassType = typeof(StubClassWithLotsOfProperties);
            var constructorInfo = GetConstructorInfo(stubClassType);
            var warmup1 = new StubClassWithLotsOfProperties { Field1 = "fred" };
            var warmup2 = ActivationTestBed<StubClassWithLotsOfProperties>(constructorInfo);

            Stopwatch origSw = new Stopwatch();
            origSw.Start();
            for (var i = 0; i < 10000; i++)
            {
                var result = new StubClassWithLotsOfProperties {Field1 = "fred"};
            }
            origSw.Stop();
            Console.WriteLine(origSw.ElapsedTicks);

            Stopwatch newSw = new Stopwatch();
            newSw.Start();
            for (var i = 0; i < 10000; i++)
            {
                var result = ActivationTestBed<StubClassWithLotsOfProperties>(constructorInfo);
            }
            newSw.Stop();
            Console.WriteLine(newSw.ElapsedTicks);

        }


        private ConstructorInfo GetConstructorInfo(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors();
            return constructors.FirstOrDefault();
        }

        private Func<StubClassWithLotsOfProperties> compiledLambda;
        public StubClassWithLotsOfProperties ActivationTestBed<T>(ConstructorInfo constructor)
        {
            if (compiledLambda != null)
            {
                return compiledLambda();
            }

            ParameterInfo[] paramsInfo = constructor.GetParameters();

            //create a single param of type object[]
            ParameterExpression param = Expression.Parameter(typeof(object[]), "args");

            var argsExp = new Expression[paramsInfo.Length];

            // Create a typed expression with each arg from the parameter array
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp = Expression.ArrayIndex(param, index);
                Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            NewExpression newExp = Expression.New(constructor, argsExp);
            Expression testExpression = Expression.MemberInit(
                newExp,
                new List<MemberBinding>()
                {
                    Expression.Bind(typeof (T).GetMember("Field1")[0],
                        Expression.Constant(GetValue()))
                });


            //create a lambda with the New Expression as the body and our param object[] as arg
            compiledLambda = Expression.Lambda<Func<StubClassWithLotsOfProperties>>(testExpression).Compile();

            // return the compiled activator
            return compiledLambda();
        }

        private string GetValue()
        {
            return "fred";
        }

    }
}





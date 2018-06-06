namespace Khala
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Kernel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AutoDataAttribute : Attribute, ITestDataSource
    {
        private readonly Lazy<IFixture> _fixtureLazy;

        public AutoDataAttribute()
            : this(new Fixture().Customize(new AutoMoqCustomization()))
        {
        }

        protected AutoDataAttribute(IFixture fixture)
        {
            if (fixture == null)
            {
                throw new ArgumentNullException(nameof(fixture));
            }

            _fixtureLazy = new Lazy<IFixture>(() => fixture, LazyThreadSafetyMode.None);
        }

        private IFixture Fixture => _fixtureLazy.Value;

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            yield return methodInfo.GetParameters().Select(Resolve).ToArray();
        }

        private object Resolve(ParameterInfo parameter)
        {
            foreach (IParameterCustomizationSource attribute in parameter
                .GetCustomAttributes()
                .OfType<IParameterCustomizationSource>())
            {
                attribute.GetCustomization(parameter).Customize(Fixture);
            }

            return new SpecimenContext(Fixture).Resolve(request: parameter);
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            IEnumerable<string> args = methodInfo.GetParameters().Zip(data, (param, arg) => $"{param.Name}: {arg}");
            return $"{methodInfo.Name}({string.Join(", ", args)})";
        }
    }
}

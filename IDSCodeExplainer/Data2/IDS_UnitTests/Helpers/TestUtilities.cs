using System.Reflection;

namespace IDS.Testing
{
    public static class TestUtilities
    {
        public static TReturn RunPrivateMethod<TClass, TReturn>(TClass instance, string privateMethodName, BindingFlags bindingFlags, params object[] parameters)
        {
            var methodInfo = typeof(TClass).GetMethod(privateMethodName, bindingFlags);
            return (TReturn)methodInfo?.Invoke(instance, parameters);
        }

        public static TReturn RunPrivateMethod<TClass, TReturn>(TClass instance, string privateMethodName, params object[] parameters)
        {
            return RunPrivateMethod<TClass, TReturn>(instance, privateMethodName, BindingFlags.NonPublic | BindingFlags.Instance, parameters);
        }

        public static TField GetPrivateMember<TClass, TField>(TClass instance, string privateMemberName)
        {
            var fieldInfos = typeof(TClass).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var memberInfo in fieldInfos)
            {
                if (memberInfo.Name == privateMemberName)
                {
                    return (TField)memberInfo.GetValue(instance);
                }
            }

            return default;
        }
    }
}

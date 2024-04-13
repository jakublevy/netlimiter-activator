using System.Security.Principal;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using OpCodes = dnlib.DotNet.Emit.OpCodes;

namespace PatchNetlimiterDLL
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            if(!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
            {
                Console.WriteLine("You need to be elevated to run this program");
                return;
            }

            string dllLocation = Environment.ExpandEnvironmentVariables("%programfiles%\\Locktime Software\\NetLimiter\\Netlimiter.dll");
            bool manual = false;
            if (!File.Exists(dllLocation))
            {
                Console.WriteLine("Netlimiter.dll not found in standard location, define a custom path");
                manual = true;
                dllLocation = Console.ReadLine();
                if (!File.Exists(dllLocation))
                {
                    Console.WriteLine($"{dllLocation} does not point to an actual file");
                    return;
                }

                if (Path.GetFileName(dllLocation) != "Netlimiter.dll")
                {
                    Console.WriteLine($"{dllLocation} does not have Netlimiter.dll name");
                    return;
                }
            }

            ModuleDefMD module;
            try
            {
                module = ModuleDefMD.Load(dllLocation);
            }
            catch
            {
                Console.WriteLine($"Error loading {dllLocation}, malformed file");
                return;
            }

            int changed = 0;

            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    var instructions = method.Body?.Instructions;
                    if (method.Name == "VerifyRegData")
                    {
                        instructions.Clear();
                        instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
                        instructions.Add(OpCodes.Ret.ToInstruction());
                        ++changed;
                    }

                    if (method.Name == "SetRegistrationDataInternal")
                    {
                        for (int i = 0; i < 29; ++i)
                            instructions.RemoveAt(0);
                        ++changed;
                    }

                    if(method.Name == "VerChkUrl")
                    {
                        instructions[4].Operand = type.GetField("WebHostUrl");
                        ++changed;
                    }

                    // Change ApiUrl to localhost
                    if (instructions?.Count > 0 && instructions[0].OpCode == OpCodes.Ldstr &&
                        instructions[0].Operand.ToString() == "https://netlimiter.com")
                    {
                        instructions[0].Operand = "http://127.0.0.1";
                        ++changed;
                    }
                }
            }

            if (changed != 4)
                Console.WriteLine("Some method for patching were not found, this patch does not probably work now");

            string patchedOutputLocation = Environment.ExpandEnvironmentVariables("%programfiles%\\Locktime Software\\NetLimiter\\Netlimiter-patched.dll");
            try
            {
                SaveModule(module, patchedOutputLocation);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                File.Delete(patchedOutputLocation);
            }
            finally
            {
                if(!manual)
                    File.Move(dllLocation, dllLocation+".bak");
                File.Move(patchedOutputLocation, Environment.ExpandEnvironmentVariables("%programfiles%\\Locktime Software\\NetLimiter\\Netlimiter.dll"));

                Console.WriteLine("Successfully patched");
                Console.WriteLine("Patched DLL written to: " + Environment.ExpandEnvironmentVariables("%programfiles%\\Locktime Software\\NetLimiter\\Netlimiter.dll"));
            }
            

        }
        private static void SaveModule(ModuleDefMD module, string fileName)
        {
            try
            {
                module.NativeWrite(fileName, new NativeModuleWriterOptions(module, false)
                {
                    Logger = DummyLogger.NoThrowInstance,
                    MetadataOptions = { Flags = MetadataFlags.PreserveAll }
                });

            }
            catch (Exception err)
            {
                Console.WriteLine($"\nFailed to save file.\n{err.Message}");
            }
        }
    }
}
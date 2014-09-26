/// This file is a part of the EBATIANOS.ESSENTIALS class library.
/// (c)2013-2014 EBATIANO'S a.k.a. Sebastian Sejud. All rights reserved.
///
/// THIS SOURCE FILE IS THE PROPERTY OF EBATIANO'S A.K.A. SEBASTIAN SEJUD 
/// AND IS NOT TO BE RE-DISTRIBUTED BY ANY MEANS WHATSOEVER WITHOUT 
/// THE EXPRESSED WRITTEN CONSENT OF EBATIANO'S A.K.A. SEBASTIAN SEJUD.
///
/// THIS SOURCE CODE CAN ONLY BE USED UNDER THE TERMS AND CONDITIONS OUTLINED
/// IN THE EBATIANOS.ESSENTIALS LICENSE AGREEMENT. 
/// EBATIANO'S A.K.A. SEBASTIAN SEJUD GRANTS TO YOU (ONE SOFTWARE DEVELOPER) 
/// THE LIMITED RIGHT TO USE THIS SOFTWARE ON A SINGLE COMPUTER.
///
/// CONTACT INFORMATION:
/// contact@ebatianos.com
/// www.ebatianos.com/essentials-library
/// 
///---------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Ebatianos.Settings
{
    /// <summary>
    /// Serialization automated singleton base, which serializes all fields of derrived type to isolated storage. 
    /// </summary>
    /// <typeparam name="T">Type to be serialized, it must be the type you derrive from this class.</typeparam>
    public abstract class SingletonSettings<T>
    {
        // Defines if type has been loaded or created.
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public Boolean Loaded { get; private set; }

        // Instance of type.
        private static T _instance;

        // Flag to avoid creation of an instance using new.
        private static Boolean _allowCreation = false;

        // Lock object for synchronization.
        private static Object _lockObj = new Object();

        // Returns instance of singleton class.
        public static T Instance
        {
            get
            {
                lock (_lockObj)
                {
                    // If no instance yet, try load it from file, otherwise create new one.
                    if (_instance == null)
                    {
                        // Set created flag to true to avoid wild creation of class.
                        _allowCreation = true;

                        // Path is full type name.
                        var path = Serializator.PathFromType(typeof(T));

                        // Try deserialze.
                        _instance = (T)Serializator.Deserialize<T>(path);

                        // If not deserialized create instance of type and set it wasn't loaded.
                        if (_instance == null)
                        {
                            _instance = (T)Activator.CreateInstance(typeof(T));

                            (_instance as SingletonSettings<T>).Loaded = false;
                        }
                        else
                        {
                            // Set that settings have been loaded.
                            (_instance as SingletonSettings<T>).Loaded = true;
                        }

                        _allowCreation = false;
                    }

                    // return singleton instance.
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Protected constructor throwing if additional not singleton instance is created.
        /// </summary>
        protected SingletonSettings()
        {
            lock (_lockObj)
            {
                if (!_allowCreation)
                {
                    throw new Exception("Cannot create an instance of singleton.");
                }
            }
        }

        /// <summary>
        /// Serializes setings.
        /// </summary>
        public void Serialize()
        {
            // Create path.
            var path = Serializator.PathFromType(typeof(T));

            // Serialize with serializator.
            Serializator.Serialize(path, Instance);
        }
    }
}

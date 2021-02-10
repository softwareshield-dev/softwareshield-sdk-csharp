/**
 * SDK 5.3 API C# wrapper
 *
 * 
 * Change log:
 * 
 * [June 06, 2015]
 *  + adds new apis for MovePackage
 * 
 * 
 * [Mar. 18, 2015]
 *  + adds online activation apis: gsIsSNValid / gsApplySN /gsIsServerAlive
 *    The online activation is now using native gsCore apis, the .Net based implementation can be enabled
 *    by define ONLINE_ACTIVATION_DOTNET
 *
 *   bool TGSCore.applySerialNumber(string sn);
 *   bool TGSCore.isSerialNumberValid(string sn);
 *   bool TGSCore.isServerAlive();
 *  
 *   void TGSCore.lockAllEntities();
 *
 *  + new api:  Retrieve the license of an entity
 *   TGSLicense TGSEntity.getLicense();
 *  
 *   Since SDK5.3, only one license can be bundled with an entity, so the old apis: getLicenseByIndex / getLicenseById are deprecated.
 *  
 *
 * [Nov. 25, 2014]
 *   + adds in-memory license data initialize api
 *     TGSCore.init(string productId, byte[] licData, string password);
 *     
 * [Nov. 18, 2014]
 *   + adds Online activation api
 *     TGSCore.applySerialNumber(sn);
 *   
 *    to enable online activation, you must:
 *   
 *   [1] make sure ONLINE_ACTIVATION is defined;
 *   [2] adds reference of assembly Newtonsoft.Json.dll to your project.  
 *     
 * [May 28, 2013]
 *   Mono 2.6 compatible
 *   changes from import by ordinal to import by name.  ( #i => fi)
 *   
 * [Apr. 22, 2013]
 *  Sync with C++ /5.1.3 apis
 * 
 * 
 */

//---------------------------------
// Enable Online Activation Feature
//
// to enable .NET based online activation, you must also add reference of Newtonsoft.Json.dll to your project.
//
// Since SDK5.3, we use native code to do code exchange with checkpoint server by default.
//
//#define ONLINE_ACTIVATION_DOTNET

using System;
using System.Runtime.InteropServices;

#if ONLINE_ACTIVATION_DOTNET

using Newtonsoft.Json;

#endif


namespace gs
{

  #region Defination

  using gs_handle_t = System.IntPtr;

  using TEntityHandle = System.IntPtr;
  using TLicenseHandle = System.IntPtr;
  using TVarHandle = System.IntPtr;
  using TMonitorHandle = System.IntPtr;
  using TActionHandle = System.IntPtr;
  using TRequestHandle = System.IntPtr;
  using TEventHandle = System.IntPtr;
  using TEventSourceHandle = System.IntPtr;
  using TMPHandle = System.IntPtr; //Move package handle
  using TCodeExchangeHandle = System.IntPtr;

  using entity_id_t = System.String;
  using license_id_t = System.String;
  using action_id_t = System.Byte;

  using vm_mask_t = System.UInt32;

  using System.Text;
  using System.Collections.Generic;
  using System.Net;
  using System.IO;
  using System.Diagnostics;


public enum TCheckPointServerTimeout : int {
  TIMEOUT_USE_SERVER_SETTING = -1,  //Uses the server's timeout pre-defined in license project
  TIMEOUT_WAIT_INFINITE = 0         //Keep waiting for server response
};

  public enum TLicenseStatus
  {
    STATUS_INVALID = -1, //The current status value is invalid
    STATUS_LOCKED = 0, ///isValid() always return false  the license is disabled permanently.
    STATUS_UNLOCKED = 1,///isValid() always return true, it happens when fully purchased.
    STATUS_ACTIVE = 2 ///isValid() works by its own logic.
  } ;

  public enum TEventType
  {
    /* --------------------- Application Events ------------------*/
    //Events from Application, application event id range: [0, 99]
    EVENT_TYPE_APP = 0,
    EVENT_IDBASE_APPLICATION = 0,

    ///\brief Application just gets started, please initialize
    ///
    /// When this event triggers, the local license has been initialized via gsInit().
    ///
    EVENT_APP_BEGIN = 1,
    ///
    ///Application is going to terminate, last signal before game exits.
    EVENT_APP_END = 2,
    ///Alarm: Application detects the clock is rolled back
    EVENT_APP_CLOCK_ROLLBACK = 3,
    ///Fatal Error: Application integrity is corrupted.
    EVENT_APP_INTEGRITY_CORRUPT = 4,
    ///Application starts to run, last signal before game code is executing
    EVENT_APP_RUN = 5,


    /*------------ License Events ----------------------------------*/

    /// Events from License Document, License event id range: [100, 199]
    EVENT_TYPE_LICENSE = 100,
    EVENT_IDBASE_LICENSE = 100,
    ///Original license is uploaded to license store for the first time.
    EVENT_LICENSE_NEWINSTALL = 101,
    ///The application's license store is connected /initialized successfully (gsCore::gsInit() == 0)
    EVENT_LICENSE_READY = 102,

    ///The application's license store cannot be connected /initialized! (gsCore::gsInit() != 0)
    EVENT_LICENSE_FAIL = 103,
    ///License is loading...
    EVENT_LICENSE_LOADING = 105,

    /*---------------- Entity Events -------------------------*/

    /// Entity event id range [200, 299]
    EVENT_TYPE_ENTITY = 200,
    EVENT_IDBASE_ENTITY = 200,
    /**
    * The entity is to be accessed.
    *
    * The listeners might be able to modify the license store here.
    * The internal licenses status are untouched. (inactive if not accessed before)
    */
    EVENT_ENTITY_TRY_ACCESS = 201,

    /**
    * The entity is being accessed.
    *
    * The listeners can enable any protected resources here. (inject decrypting keys, etc.)
    * The internal licenses status have changed to active mode.
    */
    EVENT_ENTITY_ACCESS_STARTED = 202,

    /**
    * The entity is leaving now.
    *
    * The listeners can revoke any protected resources here. (remove injected decrypting keys, etc.)
    * Licenses are still in active mode.
    */
    EVENT_ENTITY_ACCESS_ENDING = 203,

    /**
    * The entity is deactivated now.
    *
    * The listeners can revoke any protected resources here. (remove injected decrypting keys, etc.)
    * Licenses are kept in inactive mode.
    */
    EVENT_ENTITY_ACCESS_ENDED = 204,

    /// Alarm: Entity access invalid (due to expiration, etc)
    EVENT_ENTITY_ACCESS_INVALID = 205,
    /// Internal ping event indicating entity is still alive.
    EVENT_ENTITY_ACCESS_HEARTBEAT = 206,

    /**
    * \brief Action Applied to Entity
    * The status of attached licenses have been modified by applying license action.
    *
    * It is called after the change has been made.
    *
    */
    EVENT_ENTITY_ACTION_APPLIED = 208,


    /* ----------------- User Defined Events -----------------*/
    EVENT_TYPE_USER = 0x10000000,  ///< User Defined Event Id Range: [0x10000000, 0xFFFFFFFF)
    EVENT_IDBASE_USER = 0x10000000,

    ///User defined event id must >= GS_USER_EVENT
    GS_USER_EVENT = EVENT_IDBASE_USER,
    GS_USER_EVENT_1 = GS_USER_EVENT + 1,
    GS_USER_EVENT_2 = GS_USER_EVENT + 2,
    GS_USER_EVENT_3 = GS_USER_EVENT + 3,
    GS_USER_EVENT_4 = GS_USER_EVENT + 4,
    GS_USER_EVENT_5 = GS_USER_EVENT + 5,
    GS_USER_EVENT_6 = GS_USER_EVENT + 6,
    GS_USER_EVENT_7 = GS_USER_EVENT + 7,
    GS_USER_EVENT_8 = GS_USER_EVENT + 8,
    GS_USER_EVENT_9 = GS_USER_EVENT + 9
  };

  public enum TRunMode { RM_SDK, RM_WRAP } ;

  public enum TGSError
  {
    GS_ERROR_GENERIC = -1,
    GS_ERROR_INVALID_HANDLE = 1, /* null handle */
    GS_ERROR_INVALID_INDEX = 2, /* Index out of range */
    GS_ERROR_INVALID_NAME = 3, /* Invalid Variable Name */
    GS_ERROR_INVALID_ACTION = 4, /* Invalid action for target license */
    GS_ERROR_INVALID_LICENSE = 5, /* Invalid license for target entity */
    GS_ERROR_INVALID_ENTITY = 6, /* Invalid entity for application */
    GS_ERROR_INVALID_VALUE = 7 /* Invalid variable value */
  };

  //Virtual Machine Type
  [Flags]
  public enum TVirtualMachine : uint
  {
    VM_VMware = 0x1,        /// VMware (http://www.vmware.com/)
    VM_VirtualPC = 0x2,     /// Virtual PC (http://www.microsoft.com/windows/virtual-pc/)
    VM_VirtualBox = 0x4,    /// VirtualBox (https://www.virtualbox.org/)
    VM_Fusion = 0x8,        /// VMWARE Fusion
    VM_Parallel = 0x10,     /// Parallels (http://www.parallels.com)
    VM_QEMU = 0x20,         /// QEMU (http://www.qemu.org)
    VM_All = 0xFF
  };

  // Entity Status Attributes
  [Flags]
  public enum TEntityAttr : uint
  {
    /// Entity is currently accessible.
    ENTITY_ATTRIBUTE_ACCESSIBLE = 1,
    /// Entity's license is fully activated, no expire /trial limits at all.
    ENTITY_ATTRIBUTE_UNLOCKED = 2,
    /// Entity is active (being accessed via gsBeginAccessEntity())
    ENTITY_ATTRIBUTE_ACCESSING = 4,
    /// Entity is locked
    ENTITY_ATTRIBUTE_LOCKED = 8,
    /// Entity is auto-start
    ENTITY_ATTRIBUTE_AUTOSTART = 16
  };

  // License Model Property Permission 
  [Flags]
  public enum TLMParamAttr : uint
  {
    ///the param is invisible from SDK apis
    LM_PARAM_HIDDEN = 1,
    ///the param is not persistent (not saved in local license storage)
    LM_PARAM_TEMP = 2,
    ///the param can read via SDK apis
    LM_PARAM_READ = 4,
    ///the param can write via SDK apis
    LM_PARAM_WRITE = 8,
    ///the param is inheritable (new build will inherit value from old build when license upgrading)
    LM_PARAM_INHERIT = 16
  };

  // User Defined Variable Attribure Mask
  [Flags]
  public enum TVarAttr : uint
  {
    /// Variable is readable 
    VAR_ATTR_READ = 0x01,
    /// Variable is writable
    VAR_ATTR_WRITE = 0x02,
    /// Variable is persisted to local storage
    VAR_ATTR_PERSISTENT = 0x04,
    /// Variable is secured in memory
    VAR_ATTR_SECURE = 0x08,
    /// Variable is persisted at server side
    VAR_ATTR_REMOTE = 0x10,
    /// Variable cannot be enumerted via apis
    VAR_ATTR_HIDDEN = 0x20,
    /// Variable is reserved for internal system usage
    VAR_ATTR_SYSTEM = 0x40
  };

  /** \brief User Defined Variable TypeId
  *  
  * Ref: \ref varType \ref gs::gsAddVariable()
  * \anchor TVarType
  */
  public enum TVarType : uint
  {
    VAR_TYPE_INT = 7, ///< 32-bit integer
    VAR_TYPE_INT64 = 8, ///< 64-bit integer
    VAR_TYPE_FLOAT = 9, ///< float
    VAR_TYPE_DOUBLE = 10, ///< double
    VAR_TYPE_BOOL = 11, ///Boolean
    VAR_TYPE_STRING = 20, ///ansi-string
    VAR_TYPE_TIME = 30 /// UTC date time
  };

  /**
    Event Callback.
  */
  [UnmanagedFunctionPointerAttribute(CallingConvention.Winapi)]
  public delegate void gs5_monitor_callback(int evtId, TEntityHandle hEvent, IntPtr userData);
  #endregion

  #region PInvoke
  public static class GS5_Intf
  {
    public static IntPtr INVALID_GS_HANDLE = IntPtr.Zero;

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f2")]
    private static extern IntPtr _gsGetVersion();
    public static string gsGetVersion()
    {
      return Marshal.PtrToStringAnsi(_gsGetVersion());
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f3")]
    private static extern int _gsInit(string productId, string origLic, string password, IntPtr reserved);


    public static int gsInit(string productId, string origLic, string password)
    {
      return _gsInit(productId, origLic, password, IntPtr.Zero);
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f103")]
    private static extern int _gsInitEx(string productId, byte[] origLic, int licSize, string password, IntPtr reserved);


    public static int gsInit(string productId, byte[] origLic, string password)
    {
      return _gsInitEx(productId, origLic, origLic.Length, password, IntPtr.Zero);
    }

    [DllImport("gsCore", EntryPoint = "f4")]
    public static extern int gsCleanUp();

    [DllImport("gsCore", EntryPoint = "f5")]
    public static extern void gsCloseHandle(IntPtr handle);

    [DllImport("gsCore", EntryPoint = "f6")]
    public static extern void gsFlush();

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f7")]
    private static extern IntPtr _gsGetLastErrorMessage();
    public static string gsGetLastErrorMessage()
    {
      return Marshal.PtrToStringAnsi(_gsGetLastErrorMessage());
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f104")]
    public static extern void gsSetLastErrorInfo(int errCode, string errMsg);

    [DllImport("gsCore", EntryPoint = "f8")]
    public static extern int gsGetLastErrorCode();

    [DllImport("gsCore", EntryPoint = "f9")]
    public static extern int gsGetBuildId();

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f84")]
    private static extern IntPtr _gsGetProductName();
    public static string gsGetProductName()
    {
      return Marshal.PtrToStringAnsi(_gsGetProductName());
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f85")]
    private static extern IntPtr _gsGetProductId();
    public static string gsGetProductId()
    {
      return Marshal.PtrToStringAnsi(_gsGetProductId());
    }

    //Entity
    [DllImport("gsCore", EntryPoint = "f10")]
    public static extern int gsGetEntityCount();

    [DllImport("gsCore", EntryPoint = "f11")]
    public static extern TEntityHandle gsOpenEntityByIndex(int index);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f12")]
    public static extern TEntityHandle gsOpenEntityById(entity_id_t entityId);

    [DllImport("gsCore", EntryPoint = "f13")]
    public static extern UInt32 gsGetEntityAttributes(TEntityHandle hEntity);

    [DllImport("gsCore", EntryPoint = "f14")]
    private static extern IntPtr _gsGetEntityId(TEntityHandle hEntity);
    public static entity_id_t gsGetEntityId(TEntityHandle hEntity)
    {
      return Marshal.PtrToStringAnsi(_gsGetEntityId(hEntity));
    }

    [DllImport("gsCore", EntryPoint = "f15")]
    private static extern IntPtr _gsGetEntityName(TEntityHandle hEntity);
    public static string gsGetEntityName(TEntityHandle hEntity)
    {
      return Marshal.PtrToStringAnsi(_gsGetEntityName(hEntity));
    }

    [DllImport("gsCore", EntryPoint = "f16")]
    private static extern IntPtr _gsGetEntityDescription(TEntityHandle hEntity);
    public static string gsGetEntityDescription(TEntityHandle hEntity)
    {
      return Marshal.PtrToStringAnsi(_gsGetEntityDescription(hEntity));
    }

    [DllImport("gsCore", EntryPoint = "f20")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsBeginAccessEntity(TEntityHandle hEntity);

    [DllImport("gsCore", EntryPoint = "f21")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsEndAccessEntity(TEntityHandle hEntity);

    //License
    [DllImport("gsCore", EntryPoint = "f22")]
    private static extern IntPtr _gsGetLicenseName(TLicenseHandle hLicense);
    public static string gsGetLicenseName(TLicenseHandle hLicense)
    {
      return Marshal.PtrToStringAnsi(_gsGetLicenseName(hLicense));
    }

    [DllImport("gsCore", EntryPoint = "f23")]
    private static extern IntPtr _gsGetLicenseDescription(TLicenseHandle hLicense);
    public static string gsGetLicenseDescription(TLicenseHandle hLicense)
    {
      return Marshal.PtrToStringAnsi(_gsGetLicenseDescription(hLicense));
    }

    [DllImport("gsCore", EntryPoint = "f24")]
    public static extern TLicenseStatus gsGetLicenseStatus(TLicenseHandle hLicense);

    [Obsolete("API is deprecated in SDK5.3, please use gsHasLicense() instead")]
    [DllImport("gsCore", EntryPoint = "f25")]
    public static extern int gsGetLicenseCount(TEntityHandle hEntity);

    [Obsolete("API is deprecated in SDK5.3, please use gsOpenLicense() instead")]
    [DllImport("gsCore", EntryPoint = "f26")]
    public static extern TLicenseHandle gsOpenLicenseByIndex(TEntityHandle hEntity, int index);

    [Obsolete("API is deprecated in SDK5.3, please use gsOpenLicense() instead")]
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f27")]
    public static extern TLicenseHandle gsOpenLicenseById(TEntityHandle hEntity, license_id_t licenseId);
    /*
    * Inspect the license model's status
    */
    [DllImport("gsCore", EntryPoint = "f28")]
    private static extern IntPtr _gsGetLicenseId(TLicenseHandle hLicense);
    public static license_id_t gsGetLicenseId(TLicenseHandle hLicense)
    {
      return Marshal.PtrToStringAnsi(_gsGetLicenseId(hLicense));
    }

    [DllImport("gsCore", EntryPoint = "f29")]
    public static extern int gsGetLicenseParamCount(TLicenseHandle hLicense);

    [DllImport("gsCore", EntryPoint = "f30")]
    public static extern TVarHandle gsGetLicenseParamByIndex(TLicenseHandle hLicense, int index);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f31")]
    public static extern TVarHandle gsGetLicenseParamByName(TLicenseHandle hLicense, string name);

    [DllImport("gsCore", EntryPoint = "f32")]
    public static extern int gsGetActionInfoCount(TLicenseHandle hLicense);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f33")]
    private static extern IntPtr _gsGetActionInfoByIndex(TLicenseHandle hLicense, int index, out action_id_t actionId);
    public static string gsGetActionInfoByIndex(TLicenseHandle hLicense, int index, out action_id_t actionId)
    {
      return Marshal.PtrToStringAnsi(_gsGetActionInfoByIndex(hLicense, index, out actionId));
    }

    [DllImport("gsCore", EntryPoint = "f34")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsLicenseValid(TLicenseHandle hLicense);

    [DllImport("gsCore", EntryPoint = "f48")]
    public static extern TEntityHandle gsGetLicensedEntity(TLicenseHandle hLicense);

    /**
     *	Inspect an action
     */
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f38")]
    private static extern IntPtr _gsGetActionName(TActionHandle hAct);
    public static string gsGetActionName(TActionHandle hAct)
    {
      return Marshal.PtrToStringAnsi(_gsGetActionName(hAct));
    }

    [DllImport("gsCore", EntryPoint = "f39")]
    public static extern action_id_t gsGetActionId(TActionHandle hAct);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f40")]
    private static extern IntPtr _gsGetActionDescription(TActionHandle hAct);
    public static string gsGetActionDescription(TActionHandle hAct)
    {
      return Marshal.PtrToStringAnsi(_gsGetActionDescription(hAct));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f41")]
    private static extern IntPtr _gsGetActionString(TActionHandle hAct);
    public static string gsGetActionString(TActionHandle hAct)
    {
      return Marshal.PtrToStringAnsi(_gsGetActionString(hAct));
    }

    /**
     * Inspect action's parameters
     */
    [DllImport("gsCore", EntryPoint = "f42")]
    public static extern int gsGetActionParamCount(TActionHandle hAct);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f43")]
    public static extern TVarHandle gsGetActionParamByName(TActionHandle hAct, string paramName);

    [DllImport("gsCore", EntryPoint = "f44")]
    public static extern TVarHandle gsGetActionParamByIndex(TActionHandle hAct, int index);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f144")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsRevokeSN(int timeout, string serialNumber);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f135")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsRevokeApp(int timeout, string serialNumber);

    //Variables
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f50")]
    public static extern TVarHandle gsAddVariable(string varName, TVarType varType, uint varAttr, string initValStr);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f51")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsRemoveVariable(string varName);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f52")]
    public static extern TVarHandle gsGetVariable(string varName);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f53")]
    private static extern IntPtr _gsGetVariableName(TVarHandle hVar);
    public static string gsGetVariableName(TVarHandle hVar)
    {
      return Marshal.PtrToStringAnsi(_gsGetVariableName(hVar));
    }

    [DllImport("gsCore", EntryPoint = "f54")]
    public static extern TVarType gsGetVariableType(TVarHandle hVar);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f55")]
    private static extern IntPtr _gsVariableTypeToString(TVarType paramType);
    public static string gsVariableTypeToString(TVarType paramType)
    {
      return Marshal.PtrToStringAnsi(_gsVariableTypeToString(paramType));
    }

    [DllImport("gsCore", EntryPoint = "f56")]
    public static extern uint gsGetVariableAttribute(TVarHandle hVar);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f65")]
    private static extern IntPtr _gsVariableAttributeToString(uint varAttr, StringBuilder buf, int bufSize);
    public static string gsVariableAttributeToString(uint varAttr, StringBuilder buf, int bufSize)
    {
      return Marshal.PtrToStringAnsi(_gsVariableAttributeToString(varAttr, buf, bufSize));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f66")]
    public static extern uint gsVariableAttributeFromString(string attrStr);


    //Value Get/Set
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f57")]
    private static extern IntPtr _gsGetVariableValueAsString(TVarHandle hVar);
    public static string gsGetVariableValueAsString(TVarHandle hVar)
    {
      return Marshal.PtrToStringAnsi(_gsGetVariableValueAsString(hVar));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f58")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsSetVariableValueFromString(TVarHandle hVar, string valstr);

    [DllImport("gsCore", EntryPoint = "f59")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsGetVariableValueAsInt(TVarHandle hVar, out int val);

    [DllImport("gsCore", EntryPoint = "f60")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsSetVariableValueFromInt(TVarHandle hVar, int val);

    [DllImport("gsCore", EntryPoint = "f61")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsGetVariableValueAsInt64(TVarHandle hVar, out Int64 val);

    [DllImport("gsCore", EntryPoint = "f62")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsSetVariableValueFromInt64(TVarHandle hVar, Int64 val);

    [DllImport("gsCore", EntryPoint = "f63")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsGetVariableValueAsFloat(TVarHandle hVar, out float val);

    [DllImport("gsCore", EntryPoint = "f64")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsSetVariableValueFromFloat(TVarHandle hVar, float val);

    [DllImport("gsCore", EntryPoint = "f78")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsGetVariableValueAsDouble(TVarHandle hVar, out double val);

    [DllImport("gsCore", EntryPoint = "f79")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsSetVariableValueFromDouble(TVarHandle hVar, double val);

    [DllImport("gsCore", EntryPoint = "f67")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsVariableValid(TVarHandle hVar);

    [DllImport("gsCore", EntryPoint = "f68")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsGetVariableValueAsTime(TVarHandle hVar, out Int64 val);

    [DllImport("gsCore", EntryPoint = "f69")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsSetVariableValueFromTime(TVarHandle hVar, Int64 val);

    //Request
    [DllImport("gsCore", EntryPoint = "f36")]
    public static extern TRequestHandle gsCreateRequest();

    [DllImport("gsCore", EntryPoint = "f37")]
    public static extern TActionHandle gsAddRequestAction(TRequestHandle hReq, action_id_t actId, TLicenseHandle hLic);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f47")]
    public static extern TActionHandle gsAddRequestActionEx(TRequestHandle hReq, action_id_t actId, string entityId, string licenseId);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f45")]
    private static extern IntPtr _gsGetRequestCode(TRequestHandle hReq);
    public static string gsGetRequestCode(TRequestHandle hReq)
    {
      return Marshal.PtrToStringAnsi(_gsGetRequestCode(hReq));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f46")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsApplyLicenseCode(string licenseCode);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f158")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsApplyLicenseCodeEx(string licenseCode, string sn, string snRef);


    //---------- Time Engine Service ------------
    [DllImport("gsCore", EntryPoint = "f70")]
    public static extern void gsTurnOnInternalTimer();

    [DllImport("gsCore", EntryPoint = "f71")]
    public static extern void gsTurnOffInternalTimer();

    [DllImport("gsCore", EntryPoint = "f72")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsInternalTimerActive();

    [DllImport("gsCore", EntryPoint = "f73")]
    public static extern void gsTickFromExternalTimer();

    [DllImport("gsCore", EntryPoint = "f74")]
    public static extern void gsPauseTimeEngine();

    [DllImport("gsCore", EntryPoint = "f75")]
    public static extern void gsResumeTimeEngine();

    [DllImport("gsCore", EntryPoint = "f76")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsTimeEngineActive();

    //Monitor
    [DllImport("gsCore", EntryPoint = "f90")]
    public static extern TMonitorHandle gsCreateMonitorEx(gs5_monitor_callback cbMonitor, IntPtr usrData, string monitorName);

    [DllImport("gsCore", EntryPoint = "f86")]
    public static extern int gsGetEventId(TEventHandle hEvent);

    [DllImport("gsCore", EntryPoint = "f87")]
    public static extern TEventType gsGetEventType(TEventHandle hEvent);

    [DllImport("gsCore", EntryPoint = "f88")]
    public static extern TEventSourceHandle gsGetEventSource(TEventHandle hEvent);

    //HTML
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f80")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsRenderHTML(string url, string title, int width, int height);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f83")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsRenderHTMLEx(string url, string title, int width, int height, bool resizable, bool exitAppWhenUIClosed, bool cleanUpAfterRendering);

    //Environment
    [DllImport("gsCore", EntryPoint = "f81")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsRunInWrappedMode();

    [DllImport("gsCore", EntryPoint = "f82")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsRunInsideVM(vm_mask_t vmask);

    //Debug Helpers (5.0.14.0+)
    [DllImport("gsCore", EntryPoint = "f91")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsDebugVersion();

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f92")]
    public static extern void gsTrace(string msg);

    /** @name User Defined Event APIs */
    //@{
    /**\brief Post User Event
    *
    * \param eventId User defined event id ( must >= GS_USER_EVENT )  
    * \param bSync true if event is posted synchronized, the api returns after the event has been parsed by all event handlers.
           otherwise the api returns immediately.
    * \param eventData [Optional] data buffer pointer associated with the event, NULL if no event data   
    * \param eventDataSize size of event data buffer, ignored if \a eventData is NULL
    *
    * \return none
    */
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f89")]
    public static extern void gsPostUserEvent(uint eventId, bool bSync, IntPtr eventData, uint eventDataSize);
    /** \brief Gets user defined event data information
    *
    * \param hEvent The handle to user event
    * \param[out] evtDataSize output inter receiving the length of event data
    * \return Pointer to user defined event data
    */
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f124")]
    public static extern IntPtr gsGetUserEventData(TEventHandle hEvent, out uint evtDataSize);


    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f122")]
    public static extern int gsGetTotalVariables();

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f123")]
    public static extern TVarHandle gsGetVariableByIndex(int index);

    //(5.2.1.0)
    [DllImport("gsCore", EntryPoint = "f130")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsAppFirstLaunched();
    //5.3.0
    [DllImport("gsCore", EntryPoint = "f131")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsServerAlive(int timeout);

    [DllImport("gsCore", EntryPoint = "f133")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool _gsApplySN(string serialNumber, out int retCode, out IntPtr pSNRef, int timeout);
    public static bool gsApplySN(string serialNumber, out int retCode, out string snRef, int timeout = (int)TCheckPointServerTimeout.TIMEOUT_USE_SERVER_SETTING)
    {
      retCode = -1;
      snRef = "";

      IntPtr ptr;
      if(_gsApplySN(serialNumber, out retCode, out ptr, timeout))
      {
        snRef = Marshal.PtrToStringAnsi(ptr);
        return true;
      }
      return false;
    }

    [DllImport("gsCore", EntryPoint = "f139")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsIsSNValid(string serialNumber, int timeout);

    [DllImport("gsCore", EntryPoint = "f136")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsHasLicense(TEntityHandle hEntity);

    [DllImport("gsCore", EntryPoint = "f137")]
    public static extern TLicenseHandle gsOpenLicense(TEntityHandle hEntity);

    [DllImport("gsCore", EntryPoint = "f138")]
    public static extern void gsLockLicense(TLicenseHandle hLic);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f155")]
    private static extern IntPtr _gsGetPreliminarySN();
    public static string gsGetPreliminarySN()
    {
      return Marshal.PtrToStringAnsi(_gsGetPreliminarySN());
    }
    //Move Package
    [DllImport("gsCore", EntryPoint = "f145")]
    public static extern TMPHandle gsMPCreate(int reserved = 0);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f146")]
    public static extern void gsMPAddEntity(TMPHandle hMovePackage, string entityId);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f147")]
    private static extern IntPtr _gsMPExport(TMPHandle hMovePackage);
    public static string gsMPExport(TMPHandle hMovePackage)
    {
      return Marshal.PtrToStringAnsi(_gsMPExport(hMovePackage));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f148")]
    private static extern IntPtr _gsMPUpload(TMPHandle hMovePackage, string sn, int timeout);
    public static string gsMPUpload(TMPHandle hMovePackage, string sn, int timeout)
    {
      return Marshal.PtrToStringAnsi(_gsMPUpload(hMovePackage, sn, timeout));
    }

    [DllImport("gsCore", EntryPoint = "f157")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsMPIsTooBigToUpload(TMPHandle hMovePackage);


    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f149")]
    public static extern TMPHandle gsMPOpen(string exportedStr);


    [DllImport("gsCore", EntryPoint = "f156")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsMPCanPreliminarySNResolved(TMPHandle hMovePackage);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f141")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsMPImportOnline(TMPHandle hMovePackage, string sn, int timeout);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f150")]
    private static extern IntPtr _gsMPGetImportOfflineRequestCode(TMPHandle hMovePackage);
    public static string gsMPGetImportOfflineRequestCode(TMPHandle hMovePackage)
    {
      return Marshal.PtrToStringAnsi(_gsMPGetImportOfflineRequestCode(hMovePackage));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f151")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool gsMPImportOffline(TMPHandle hMovePackage, string licenseCode);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f152")]
    private static extern IntPtr _gsMPUploadApp(string sn, int timeout);
    public static string gsMPUploadApp(string sn, int timeout)
    {
      return Marshal.PtrToStringAnsi(_gsMPUploadApp(sn, timeout));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f153")]
    private static extern IntPtr _gsMPExportApp();
    public static string gsMPExportApp()
    {
      return Marshal.PtrToStringAnsi(_gsMPExportApp());
    }

    //Code Exchange Gateway
    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f159")]
    public static extern TCodeExchangeHandle gsCodeExchangeBegin();


    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f160")]
    private static extern IntPtr _gsCodeExchangeGetLicenseCode(TCodeExchangeHandle hCodeExchange, string productId, int buildId, string sn, string requestCode);
    public static string gsCodeExchangeGetLicenseCode(TCodeExchangeHandle hCodeExchange, string productId, int buildId, string sn, string requestCode)
    {
      return Marshal.PtrToStringAnsi(_gsCodeExchangeGetLicenseCode(hCodeExchange, productId, buildId, sn, requestCode));
    }

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f161")]
    public static extern int gsCodeExchangeGetErrorCode(TCodeExchangeHandle hCodeExchange);

    [DllImport("gsCore", CharSet = CharSet.Ansi, EntryPoint = "f162")]
    private static extern IntPtr _gsCodeExchangeGetErrorMessage(TCodeExchangeHandle hCodeExchange);
    public static string gsCodeExchangeGetErrorMessage(TCodeExchangeHandle hCodeExchange)
    {
      return Marshal.PtrToStringAnsi(_gsCodeExchangeGetErrorMessage(hCodeExchange));
    }




  }

  #endregion PInvoke

  //------------------------ OOP -----------------------------------------------

  public class gs5_error : Exception
  {
    private TGSError _code;

    public gs5_error(string msg, TGSError code)
      : base(msg)
    {
      _code = code;
    }

    public static void raise(TGSError code, string fmt, params object[] args)
    {
      throw new gs5_error(String.Format(fmt, args), code);
    }

    public TGSError code
    {
      get { return _code; }
    }
  }

  /**
  *  TGSObject : base of all GSCore handle-based objects
  */
  public class TGSObject : IDisposable
  {
    protected gs_handle_t _handle = IntPtr.Zero;
    private bool _disposed;

    protected TGSObject()
    {
      _disposed = false;
    }

    protected internal TGSObject(gs_handle_t hObj)
      : this()
    {
      _handle = hObj;
    }

    private void cleanUp()
    {
      if (!_disposed)
      {
        if (_handle != IntPtr.Zero) GS5_Intf.gsCloseHandle(_handle);
        _disposed = true;
      }
    }

    public void Dispose()
    {
      cleanUp();
      GC.SuppressFinalize(this);
    }

    ~TGSObject()
    {
      cleanUp();
    }

    public void assertValid()
    {
      if ((_handle == IntPtr.Zero) || (_disposed)) throw new gs5_error("Invalid handle!", TGSError.GS_ERROR_INVALID_HANDLE);
    }

    protected internal gs_handle_t Handle
    {
      get { return _handle; }
    }
  };

  #region Variable
  //User defined variables or Parameters of action / licenses.
  public class TGSVariable : TGSObject
  {
    internal TGSVariable(gs_handle_t handle) : base(handle) { }

    //--- Static helpers ---
    //
    public static string getTypeName(TVarType varType)
    {
      return GS5_Intf.gsVariableTypeToString(varType);
    }

    //Attribute conversion helpers
    public static uint AttrFromString(string attrStr)
    {
      return GS5_Intf.gsVariableAttributeFromString(attrStr);
    }
    public static string AttrToString(uint varAttr)
    {
      StringBuilder sb = new StringBuilder(32);
      GS5_Intf.gsVariableAttributeToString(varAttr, sb, sb.Capacity);
      return sb.ToString();
    }

    //Setter
    public void fromBool(bool v)
    {
      this.fromInt(v ? 1 : 0);
    }

    public void fromString(string v)
    {
      if (!GS5_Intf.gsSetVariableValueFromString(_handle, v))
        throw new gs5_error("String conversion error", TGSError.GS_ERROR_INVALID_VALUE);
    }
    public void fromInt(int v)
    {
      if (!GS5_Intf.gsSetVariableValueFromInt(_handle, v))
        throw new gs5_error("Int conversion error", TGSError.GS_ERROR_INVALID_VALUE);
    }
    public void fromInt64(Int64 v)
    {
      if (!GS5_Intf.gsSetVariableValueFromInt64(_handle, v))
        throw new gs5_error("Int64 conversion error", TGSError.GS_ERROR_INVALID_VALUE);
    }
    public void fromFloat(float v)
    {
      if (!GS5_Intf.gsSetVariableValueFromFloat(_handle, v))
        throw new gs5_error("Float conversion error", TGSError.GS_ERROR_INVALID_VALUE);
    }
    public void fromDouble(double v)
    {
      if (!GS5_Intf.gsSetVariableValueFromDouble(_handle, v))
        throw new gs5_error("Double conversion error", TGSError.GS_ERROR_INVALID_VALUE);
    }

    public void fromUTCTime(DateTime dt)
    {
      Int64 t = (Int64)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
      if (!GS5_Intf.gsSetVariableValueFromTime(_handle, t))
        throw new gs5_error("DateTime conversion error", TGSError.GS_ERROR_INVALID_VALUE);
    }

    //Getter
    public string asString()
    {
      return GS5_Intf.gsGetVariableValueAsString(_handle);
    }

    public bool asBool()
    {
      return (this.asInt() != 0) ? true : false;
    }

    public int asInt()
    {
      int Result;
      if (!GS5_Intf.gsGetVariableValueAsInt(_handle, out Result))
        throw new gs5_error("Int conversion error", TGSError.GS_ERROR_INVALID_VALUE);
      return Result;
    }

    public Int64 asInt64()
    {
      Int64 Result;
      if (!GS5_Intf.gsGetVariableValueAsInt64(_handle, out Result))
        throw new gs5_error("Int64 conversion error", TGSError.GS_ERROR_INVALID_VALUE);
      return Result;
    }

    public float asFloat()
    {
      float Result;
      if (!GS5_Intf.gsGetVariableValueAsFloat(_handle, out Result))
        throw new gs5_error("Float conversion error", TGSError.GS_ERROR_INVALID_VALUE);
      return Result;
    }

    public double asDouble()
    {
      double Result;
      if (!GS5_Intf.gsGetVariableValueAsDouble(_handle, out Result))
        throw new gs5_error("Doubles conversion error", TGSError.GS_ERROR_INVALID_VALUE);
      return Result;
    }

    public DateTime asUTCTime()
    {
      if (hasValidDateTime())
      {
        Int64 t;
        if (GS5_Intf.gsGetVariableValueAsTime(_handle, out t))
        {
          DateTime tt = new DateTime(1970, 1, 1);
          return tt.AddSeconds(t);
        }
        else
          throw new gs5_error("Datetime conversion error", TGSError.GS_ERROR_INVALID_VALUE);

      }
      else
        throw new gs5_error("Variable is not initialized with a valid date time value", TGSError.GS_ERROR_INVALID_VALUE);

    }

    public bool hasValidDateTime()
    {
      return GS5_Intf.gsIsVariableValid(_handle);
    }


    //Properties
    public string Name
    {
      get { return GS5_Intf.gsGetVariableName(_handle); }
    }
    public TVarType VarType
    {
      get { return GS5_Intf.gsGetVariableType(_handle); }
    }
    public string Attribute
    {
      get { return AttrToString(GS5_Intf.gsGetVariableAttribute(_handle)); }
    }
  };

  #endregion

  #region Action
  public class TGSAction : TGSObject
  {
    /// Unlock entity / license
    public const int ACT_UNLOCK = 1;
    /// Lock down entity / license
    public const int ACT_LOCK = 2;
    //Sets a parameter's value
    public const int ACT_SET_PARAM = 3;
    // Enables a parameter
    public const int ACT_ENABLE_PARAM = 4;
    // Disable a parameter
    public const int ACT_DISABLE_PARAM = 5;
    /// Enable Copy protection feature (NodeLock)
    public const int ACT_ENABLE_COPYPROTECTION = 6;
    /// Disable Copy protection feature (NodeLock)
    public const int ACT_DISABLE_COPYPROTECTION = 7;

    /// Clean up local license storage 
    public const int ACT_CLEAN = 11;
    /// Dummy action, carry only client id
    public const int ACT_DUMMY = 12;


    /// Enable Demo Nag UI 
    public const int ACT_NAG_ON = 15;
    /// Disable Demo Nag UI
    public const int ACT_NAG_OFF = 16;
    /// Activation Code can be used only once 
    public const int ACT_ONE_SHOT = 17;
    /// Activation Code has a shelf time
    public const int ACT_SHELFTIME = 18;

    /// License Error Fix
    public const int ACT_FP_FIX = 19;
    public const int ACT_FIX = 19;
    /// Revoke local license
    public const int ACT_REVOKE = 20;

    /** @name LM-specific actions **/
    //LM.expire.accessTime
    /// Increase /Decrease access time (LM.expire.accessTime)
    public const int ACT_ADD_ACCESSTIME = 100;
    /// Sets access time (LM.expire.accessTime)
    public const int ACT_SET_ACCESSTIME = 101;

    //LM.expire.hardDate
    /// Sets start date (LM.expire.hardDate)
    public const int ACT_SET_STARTDATE = 102;
    /// Sets end date (LM.expire.hardDate)
    public const int ACT_SET_ENDDATE = 103;

    /// Sets maximum execution session time (LM.expire.sessionTime) 
    public const int ACT_SET_SESSIONTIME = 104;

    //LM.expire.period
    /// Sets expire period (LM.expire.period)
    public const int ACT_SET_EXPIRE_PERIOD = 105;
    /// Increases / Decreases expire period (LM.expire.period)
    public const int ACT_ADD_EXPIRE_PERIOD = 106;

    //LM.expire.duration
    /// Sets expire duration (LM.expire.duration)
    public const int ACT_SET_EXPIRE_DURATION = 107;
    /// Increases / Decreases expire duration (LM.expire.duration)
    public const int ACT_ADD_EXPIRE_DURATION = 108;


    public class TActParamAccessor
    {
      private TGSAction _act;
      public TActParamAccessor(TGSAction act) { _act = act; }

      public TGSVariable this[int index]
      {
        get { return _act.getParamByIndex(index); }
      }
    };

    private readonly int _totalParams;
    private TActParamAccessor _paramAccessor;

    internal TGSAction(gs_handle_t handle)
      : base(handle)
    {
      _totalParams = GS5_Intf.gsGetActionParamCount(_handle);
      _paramAccessor = new TActParamAccessor(this);
    }

    public TGSVariable getParamByIndex(int index)
    {
      if ((index < 0) || (index >= _totalParams))
      {
        gs5_error.raise(TGSError.GS_ERROR_INVALID_INDEX, "Index {0} out of range [0, {1})", index, _totalParams);
      };
      return new TGSVariable(GS5_Intf.gsGetActionParamByIndex(_handle, index));
    }

    public TGSVariable getParamByName(string name)
    {
      gs_handle_t h = GS5_Intf.gsGetActionParamByName(_handle, name);
      if (h == GS5_Intf.INVALID_GS_HANDLE)
        gs5_error.raise(TGSError.GS_ERROR_INVALID_NAME, "Invalid Param Name [{0}]", name);

      return new TGSVariable(h);
    }
    //Properties
    public string Name
    {
      get { return GS5_Intf.gsGetActionName(_handle); }
    }
    public action_id_t Id
    {
      get { return GS5_Intf.gsGetActionId(_handle); }
    }

    public string Description
    {
      get { return GS5_Intf.gsGetActionDescription(_handle); }
    }

    public string WhatToDo
    {
      get { return GS5_Intf.gsGetActionString(_handle); }
    }

    public int ParamCount
    {
      get { return _totalParams; }
    }

    /// <summary>
    /// 
    /// TGSVariable v = act.Params[0];
    /// </summary>
    public TActParamAccessor Params
    {
      get { return _paramAccessor; }
    }
  };
  #endregion

  #region License
  //License
  public class TGSLicense : TGSObject
  {
    public class TLicParamAccessor
    {
      private TGSLicense _lic;
      internal TLicParamAccessor(TGSLicense lic) { _lic = lic; }

      public TGSVariable this[int index]
      {
        get
        {
          return _lic.getParamByIndex(index);
        }
      }
      public TGSVariable this[string paramName]
      {
        get
        {
          return _lic.getParamByName(paramName);
        }
      }
    }

    public class TActIdAccessor
    {
      private TGSLicense _lic;
      internal TActIdAccessor(TGSLicense lic) { _lic = lic; }

      public action_id_t this[int index]
      {
        get
        {
          return _lic.actionIds(index);
        }
      }
    }

    public class TActNameAccessor
    {
      private TGSLicense _lic;
      internal TActNameAccessor(TGSLicense lic) { _lic = lic; }

      public string this[int index]
      {
        get
        {
          return _lic.actionNames(index);
        }
      }
    }

    private readonly TGSEntity _licensedEntity;
    private readonly int _totalParams;
    private readonly int _totalActs;
    private readonly TLicParamAccessor _paramAccessor;
    private readonly TActIdAccessor _actIdAccessor;
    private readonly TActNameAccessor _actNameAccessor;


    internal TGSLicense(TGSEntity entity, gs_handle_t handle)
      : base(handle)
    {
      _licensedEntity = entity;
      _totalParams = GS5_Intf.gsGetLicenseParamCount(_handle);
      _totalActs = GS5_Intf.gsGetActionInfoCount(_handle);
      _paramAccessor = new TLicParamAccessor(this);
      _actIdAccessor = new TActIdAccessor(this);
      _actNameAccessor = new TActNameAccessor(this);
    }

    public TGSVariable getParamByIndex(int index)
    {
      if ((index < 0) || (index >= _totalParams))
      {
        gs5_error.raise(TGSError.GS_ERROR_INVALID_INDEX, "Index [{0}] out of range [0, {1})", index, _totalParams);
      }
      return new TGSVariable(GS5_Intf.gsGetLicenseParamByIndex(_handle, index));
    }

    public TGSVariable getParamByName(string name)
    {
      gs_handle_t h = GS5_Intf.gsGetLicenseParamByName(_handle, name);
      if (h == GS5_Intf.INVALID_GS_HANDLE)
        gs5_error.raise(TGSError.GS_ERROR_INVALID_NAME, "Invalid Param Name [{0}]", name);

      return new TGSVariable(h);
    }

    //Properties
    public string Id
    {
      get { return GS5_Intf.gsGetLicenseId(_handle); }
    }

    public string Name
    {
      get { return GS5_Intf.gsGetLicenseName(_handle); }
    }

    public string Description
    {
      get { return GS5_Intf.gsGetLicenseDescription(_handle); }
    }

    public TLicenseStatus Status
    {
      get { return GS5_Intf.gsGetLicenseStatus(_handle); }
    }

    public TGSEntity LicensedEntity
    {
      get { return _licensedEntity; }
    }

    public bool isValid
    {
      get { return GS5_Intf.gsIsLicenseValid(_handle); }
    }

    public int ParamCount
    {
      get { return _totalParams; }
    }

    public TLicParamAccessor Params
    {
      get { return _paramAccessor; }
    }

    public int ActionCount
    {
      get { return _totalActs; }
    }

    public action_id_t actionIds(int index)
    {
      action_id_t Result;
      GS5_Intf.gsGetActionInfoByIndex(_handle, index, out Result);
      return Result;
    }
    /// <summary>
    /// id = lic.ActionIds[0];
    /// </summary>
    public TActIdAccessor ActionIds
    {
      get { return _actIdAccessor; }
    }

    public string actionNames(int index)
    {
      action_id_t dummy;
      return GS5_Intf.gsGetActionInfoByIndex(_handle, index, out dummy);
    }

    public TActNameAccessor ActionNames
    {
      get { return _actNameAccessor; }
    }

    //Common Request code helpers
    /// Gets a request code to unlock this license only.
    public string getUnlockRequestCode()
    {
      TGSRequest req = TGSCore.getInstance().createRequest();
      req.addAction(TGSAction.ACT_UNLOCK, this);
      return req.Code;
    }

    //Helpers to access license parameters
    public string getParamStr(string name)
    {
      return this.getParamByName(name).asString();
    }
    public void setParamStr(string name, string valStr)
    {
      this.getParamByName(name).fromString(valStr);
    }
    public int getParamInt(string name)
    {
      return this.getParamByName(name).asInt();
    }
    public void setParamInt(string name, int v)
    {
      this.getParamByName(name).fromInt(v);
    }
    public Int64 getParamInt64(string name)
    {
      return this.getParamByName(name).asInt64();
    }
    public void setParamInt64(string name, Int64 v)
    {
      this.getParamByName(name).fromInt64(v);
    }
    public bool getParamBool(string name)
    {
      return this.getParamByName(name).asBool();
    }
    public void setParamBool(string name, bool v)
    {
      this.getParamByName(name).fromBool(v);
    }

    public float getParamFloat(string name)
    {
      return this.getParamByName(name).asFloat();
    }
    public void setParamFloat(string name, float v)
    {
      this.getParamByName(name).fromFloat(v);
    }

    public double getParamDouble(string name)
    {
      return this.getParamByName(name).asDouble();
    }
    public void setParamDouble(string name, double v)
    {
      this.getParamByName(name).fromDouble(v);
    }

    public DateTime getParamUTCTime(string name)
    {
      return this.getParamByName(name).asUTCTime();
    }
    public void setParamUTCTime(string name, DateTime v)
    {
      this.getParamByName(name).fromUTCTime(v);
    }

  };
  #endregion

  #region Request
  //Request
  public class TGSRequest : TGSObject
  {
    internal TGSRequest(gs_handle_t handle) : base(handle) { }
    /** \brief adds a global action targeting all entities
    * 
    * Adds an action targeting the whole license storage (ACT_CLEAN), or can be applied to all entities( ACT_LOCK, ACT_UNLOCK, etc.)
    */
    public TGSAction addAction(action_id_t actId)
    {
      return this.addAction(actId, null, null);
    }

    /** \brief adds an action targeting a single license object
    *
    * \param actId Action type id;
    * \param license the target license object to which the action will be applied to;
    * \return the pointer to action object, NULL if the action type id is not supported. 
    */
    public TGSAction addAction(action_id_t actId, TGSLicense lic)
    {
      gs_handle_t h = GS5_Intf.gsAddRequestAction(_handle, actId, lic.Handle);
      if (h == GS5_Intf.INVALID_GS_HANDLE)
        gs5_error.raise(TGSError.GS_ERROR_INVALID_ACTION, "Invalid action (actId = {0}) for target license ({1})",
            actId, lic.Name);

      return new TGSAction(h);
    }
    /** \brief adds an action targeting all licenses of an entity
    *
    * \param actId Action type id;
    * \param entity the target entity, the action will be applied to all licenses attached to the entity.
    * \return the pointer to action object, NULL if the action type id is not supported. 
    */
    public TGSAction addAction(action_id_t actId, TGSEntity entity)
    {
      return this.addAction(actId, entity.Id, null);
    }
    /** \brief adds an action targeting a single license object
    *
    * \param actId Action type id;
    * \param entityId the target entity id to which the target license is attached;
    * \param licenseId the target license id to which the action will be applied to;
    * \return the pointer to action object, NULL if the action type id is not supported. 
    */
    public TGSAction addAction(action_id_t actId, string entityId, string licenseId)
    {
      gs_handle_t h = GS5_Intf.gsAddRequestActionEx(_handle, actId, entityId, licenseId);
      if (h == GS5_Intf.INVALID_GS_HANDLE)
        gs5_error.raise(TGSError.GS_ERROR_INVALID_ACTION, "Invalid action (actId = {0}) for target license ({1})", actId, licenseId);

      return new TGSAction(h);
    }

    /// gets the request string code
    public string Code
    {
      get { return GS5_Intf.gsGetRequestCode(_handle); }
    }
  };

  #endregion

  #region Entity
  //Entity
  public class TGSEntity : TGSObject
  {
    internal TGSEntity(gs_handle_t handle)
      : base(handle)
    {
    }

    /** \brief Try start accessing an entity.
     *
     * If an entity is accessible, all of the associated resources (files, keys, codes, etc.) can be legally used, otherwise
     * they cannot be accessed by the application.
     *
     * This api can be called recursively, and each call must be paired with an endAccess(). 
 
     * When the api is called an event EVENT_ENTITY_TRY_ACCESS is triggered to give the GS5 extension developer a chance to change the entity license
     * status, if after the EVENT_ENTITY_TRY_ACCESS posting the entity is still not accessible, then EVENT_ENTITY_ACCESS_INVALID is posted, otherwise,
     * if the entity is being accessed _for the very first time_, the EVENT_ENTITY_ACCESS_STARTED is posted. the developer can then initialize
     * needed resources for this entity in the event handler.
     *
     * \return 	returns true if the entity is accessed successfully.
            returns false if:
            - Cannot access any entity when your game is wrapped by a *DEMO* version of GS5/IDE and the its demo license has expired;
            - Entity cannot be accessed due to its negative license feedback;

        \see gs::gsBeginAccessEntity()
		
     */
    public bool beginAccess()
    {
      return GS5_Intf.gsBeginAccessEntity(_handle);
    }

    /** \brief Try end accessing an entity

      \return true on success, false if there is unexpected error occurs.
  
      This api must be paired with beginAccess(), if it is the last calling then event EVENT_ENTITY_ACCESS_ENDING and 
      EVENT_ENTITY_ACCESS_ENDED will be posted.

      \see gs::gsEndAccessEntity()

    */
    public bool endAccess()
    {
      return GS5_Intf.gsEndAccessEntity(_handle);
    }

    public TGSLicense getLicense()
    {
      if (GS5_Intf.gsHasLicense(_handle))
      {
        return new TGSLicense(this, GS5_Intf.gsOpenLicense(_handle));
      }
      return null;
    }

    //Common Request code helpers
    /// Gets a request code to unlock this license only.
    public string getUnlockRequestCode()
    {
      TGSRequest req = TGSCore.getInstance().createRequest();
      req.addAction(TGSAction.ACT_UNLOCK, this);
      return req.Code;
    }

    //Properties
    public UInt32 Attribute
    {
      get { return GS5_Intf.gsGetEntityAttributes(_handle); }
    }

    //****** Licensing Status *****
    ///Is Entity accessible? (Passed attached license(s) verfication) 
    public bool isAccessible() { return (this.Attribute & (uint)TEntityAttr.ENTITY_ATTRIBUTE_ACCESSIBLE) != 0; }
    ///Is Entity being accessed? ( between beginAccess & endAccess )
    public bool isAccessing() { return (this.Attribute & (uint)TEntityAttr.ENTITY_ATTRIBUTE_ACCESSING) != 0; }
    ///Is Entity unlocked? (Fully Purchased, etc.)
    public bool isUnlocked() { return (this.Attribute & (uint)TEntityAttr.ENTITY_ATTRIBUTE_UNLOCKED) != 0; }
    ///Is Entity locked? (Expired, obsoleted, etc.)
    public bool isLocked() { return (this.Attribute & (uint)TEntityAttr.ENTITY_ATTRIBUTE_LOCKED) != 0; }

    public void lockLicense()
    {
      TGSLicense lic = getLicense();
      if (lic != null)
      {
        GS5_Intf.gsLockLicense(lic.Handle);
      }
    }

    public string Id
    {
      get { return GS5_Intf.gsGetEntityId(_handle); }
    }

    public string Name
    {
      get { return GS5_Intf.gsGetEntityName(_handle); }
    }

    public string Description
    {
      get { return GS5_Intf.gsGetEntityDescription(_handle); }
    }

    public TGSLicense License
    {
      get
      {
        return getLicense();
      }
    }
  };

  #endregion

  #region MovePackage

  public class TMovePackage : TGSObject
  {
    public TMovePackage(TMPHandle hMovePackage) :base(hMovePackage){ }

    public void addEntityId(string entityId)
    {
      GS5_Intf.gsMPAddEntity(_handle, entityId);
    }

    ///------- Move License Online --------
    ///Returns a receipt ( actually a SN ) from server on success
    ///
    /// It will be used to activate app on the target machine so
    /// should be saved in a safely place.
    /// 
    /// After this api returns, the entities in this move package are locked.
    ///
    public string upload(string preSN = ""){
      //make sure we have a valid preliminary SN for online operation
      Debug.Assert(preSN != "" || canPreliminarySNResolved(), "preliminary SN unavailable for online move package uploading!");

      return GS5_Intf.gsMPUpload(_handle, preSN, (int)TCheckPointServerTimeout.TIMEOUT_WAIT_INFINITE);
    }

    /// <summary>
    /// Test if the content of this move package can be uploaded to server?
    /// 
    /// If a package is too big, the online package uploading is not supported; however, the offline package moving always work.
    /// </summary>
    /// <returns>
    /// true if the package size is supported by server
    /// </returns>
    public bool isTooBigToUpload(){
      return GS5_Intf.gsMPIsTooBigToUpload(_handle);
    }
    ///----- Move License Offline ---------
    ///Returns encrypted data string of move package
    /// It will be used to activate app on the target machine so
    /// should be saved in a safely place.
    /// 
    /// On Success:
    ///   return non-empty string, and the entities in this move package are locked.
    ///
    public string exportData(){
      return GS5_Intf.gsMPExport(_handle);
    }

    /// <summary>
    /// Generate the request code for manual importation of move package.
    /// </summary>
    /// <returns>
    /// The request code for move package importing.
    /// </returns>
    public string getImportOfflineRequestCode(){
      return GS5_Intf.gsMPGetImportOfflineRequestCode(_handle);
    }

    /// <summary>
    /// Manual import of move package data
    ///
    /// The customer sends request code to support team and receives a license code to apply.
    /// </summary>
    /// <param name="licenseCode">
    /// Generated by product support team to authorize the import at client side
    /// </param>
    /// <returns>true on success, all of the entities status are imported to the target machine.
    /// </returns>
    public bool importOffline(string licenseCode){
      return GS5_Intf.gsMPImportOffline(_handle, licenseCode);
    }

    /// <summary>
    /// Online import of move package data
    /// 
    /// </summary>
    /// <param name="preSN"></param>
    /// <returns></returns>
    public bool importOnline(string preSN = ""){
      //make sure we have a valid preliminary SN for online operation
      Debug.Assert(preSN != "" || canPreliminarySNResolved(), "preliminary SN unavailable for online move package importing!");

      return GS5_Intf.gsMPImportOnline(_handle, preSN, (int)TCheckPointServerTimeout.TIMEOUT_WAIT_INFINITE);
    }

    /// <summary>
    /// Test if the preliminary serial number can be resolved for this move package
    /// 
    /// Usually the move package comes with a preliminary serial number when it is exported from a source machine which was online-activated.
    /// If the source machine is manual activated, the serial number is unavailable in the move package, so a serial number must be provided
    /// as pass-in parameter for online importing (ref: importOnline) later on target machine.
    /// </summary>
    /// <returns>true on success</returns>
    public bool canPreliminarySNResolved(){
      return GS5_Intf.gsMPCanPreliminarySNResolved(_handle);
    }

  }
  #endregion

  #region CodeExchange
  public class TCodeExchange : TGSObject
  {
    public TCodeExchange(TCodeExchangeHandle hCodeExchange): base(hCodeExchange){}

    /// <summary>
    /// Generate License code for given request code
    /// </summary>
    /// <param name="productId">The product-Id of the software</param>
    /// <param name="buildId">The build-Id of the software; 
    /// set to -1 for the latest build, otherwise specifies the exact app build-id the requestCode is generated from.</param>
    /// <param name="sn">the valid serial number as a proof for license code generation</param>
    /// <param name="requestCode">the request code generated on the target machine where the license code will be applied to</param>
    /// <returns>non-empty license code on success</returns>
    public string getLicenseCode(string productId, int buildId, string sn, string requestCode){
      return GS5_Intf.gsCodeExchangeGetLicenseCode(_handle, productId, buildId, sn, requestCode);
    }

    /// <summary>
    /// The per-code-exchange error code
    /// </summary>
    /// <returns>0: success</returns>
    public int getErrorCode() {
      return GS5_Intf.gsCodeExchangeGetErrorCode(_handle);
    }

    /// <summary>
    /// The per-code-exchange error message
    /// </summary>
    /// <returns>the error message for the latest failed operation</returns>
    public string getErrorMessage(){
      return GS5_Intf.gsCodeExchangeGetErrorMessage(_handle);
    }

  }
  #endregion

#if ONLINE_ACTIVATION_DOTNET
  #region CheckPoint
    public static class Object
    {
        /// <summary>
        /// Converts an object to Json.
        /// </summary>
        /// <param name="obj">Object to be converted.</param>
        /// <returns>Json object.</returns>
        public static string ToJSON(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T FromJSON<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

    public class CheckPointServiceManager
    {
        public enum CheckPointResponseCode : int
        {
            NotValued = -1,
            InvalidSerialNumber = 0,
            NoActivationsRemaining = 1,
            CodeGenerated = 2,
            FormatOK = 3,
            Deactivated = 4,
            MissingMainLF = 5,
            MainLFCorrupt = 6,
            BadLPJFileName = 7,
            DeactivationIDOnly = 8,
            CodeInvalidBadId = 9,
            CodeBadFormat = 10,
            ParamOutOfRange = 11,
            FPOutOfRange = 12,
            WongParamType = 13,
            ShelfLifeOutOfRange = 14,
            InvalidAuthID = 15,
            InvalidSessionID = 16,
            CheckedIn = 17,
            InternalFailure = 18,
            ConnectionFailure = 19,
            InternalLicenseCheckFailed = 20,
            FailedToSaveUploadedFile = 50,
            FailedToSaveVariables = 51,
            InvalidLogin = 52,
            LoginSuccess = 53,
            ProjectDataUploadSuccess = 54,
            ProjectDataUploadFailure = 55,
            GenerateSNSuccess = 56,
            GenerateSNFailure = 57,
            LicenseNotFound = 58,
            UpdateSNSuccess = 59,
            UpdateSNFailure = 60,
            GetSNFailure = 61,
            GetSNSuccess = 62,
            ProjectBuildSuccess = 63,
            ProjectBuildFailure = 64,
            SNConsumed = 80,
            SNNotConsumed = 81,
            InvalidProduct = 82,
        }

        public class CheckPointJSONResponse
        {
            public CheckPointResponseCode ResponseCode { get; set; }
            public string Result { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorCode { get; set; }

            public CheckPointJSONResponse()
            {
                ResponseCode = CheckPointResponseCode.NotValued;
            }
        }

        public CheckPointJSONResponse ActivateProduct(string serialNumber, string requestCode, string productID, int buildID)
        {
            CheckPointJSONResponse result = null;
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("productID", productID);
            data.Add("serialNumber", serialNumber);
            data.Add("requestCode", requestCode);
            data.Add("buildID", buildID.ToString());
            result = CallJSONMethod("GenerateResponseCode", data);

            return result;
        }

        private CheckPointJSONResponse CallJSONMethod(string methodName, Dictionary<string, string> parameters)
        {

            StringBuilder url = new StringBuilder(string.Format("{0}/{1}?", "https://shield.yummy.net/checkpoint2/CheckPointService.svc", methodName));
            foreach (var item in parameters)
            {
                url.AppendFormat("{0}={1}&", item.Key, item.Value);
            }
            url.Remove(url.Length - 1, 1); // Remove last &

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url.ToString());

            request.Method = "GET";

            CheckPointJSONResponse result = null;
            using (Stream s = request.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    string response = sr.ReadToEnd();
                    result = response.FromJSON<CheckPointJSONResponse>();
                }
            }

            return result;
        }
    }

  #endregion
#endif

  #region Core
  public class TGSCore : IDisposable
  {
    //Timeout constants used by CheckPoint server access apis
    //
    public static int TIMEOUT_USE_SERVER_SETTING = -1;
    public static int TIMEOUT_WAIT_INFINITE = 0;

    public class TEntityAccessor
    {
      private TGSCore _core;
      internal TEntityAccessor(TGSCore core) { _core = core; }

      public int Count
      {
        get { return _core.EntityCount; }
      }
      public TGSEntity this[int index]
      {
        get { return _core.getEntityByIndex(index); }
      }
      public TGSEntity this[entity_id_t entityId]
      {
        get { return _core.getEntityById(entityId); }
      }
    }

    public class TVarAccessor
    {
      private TGSCore _core;
      internal TVarAccessor(TGSCore core) { _core = core; }

      public TGSVariable this[string varName]
      {
        get { return _core.getVariableByName(varName); }
      }
    }

    public class TTimeEngine
    {
      public void tickle()
      {
        GS5_Intf.gsTickFromExternalTimer();
      }

      public bool On
      {
        get
        {
          return GS5_Intf.gsIsTimeEngineActive();
        }
        set
        {
          if (value) GS5_Intf.gsResumeTimeEngine();
          else GS5_Intf.gsPauseTimeEngine();
        }
      }
    }

    public class TInternalTimer
    {
      public bool On
      {
        get
        {
          return GS5_Intf.gsIsInternalTimerActive();
        }
        set
        {
          if (value) GS5_Intf.gsTurnOnInternalTimer();
          else GS5_Intf.gsTurnOffInternalTimer();
        }
      }
    }

    private static TGSCore s_core = null; //Single Instance
    private int _rc;
    private int _totalEntities;
    private TEntityAccessor _entityAccessor;
    private TVarAccessor _varAccessor;
    private TInternalTimer _internalTimer;
    private TTimeEngine _timeEngine;

    public delegate void TGSAppEventHandler(int eventId);
    public delegate void TGSLicenseEventHandler(int eventId);
    public delegate void TGSEntityEventHandler(int eventId, TGSEntity entity);
    public delegate void TGSUserEventHandler(int eventId, IntPtr pEventData, uint dataSize);

    public event TGSAppEventHandler AppEventHandler;
    public event TGSLicenseEventHandler LicenseEventHandler;
    public event TGSEntityEventHandler EntityEventHandler;
    public event TGSUserEventHandler UserEventHandler;

    private void OnEvent(int eventId, TEventHandle hEvent)
    {
      TEventType evtType = GS5_Intf.gsGetEventType(hEvent);
      switch (evtType)
      {
        case TEventType.EVENT_TYPE_APP:
          {
            if (AppEventHandler != null) AppEventHandler(eventId);
            break;
          }
        case TEventType.EVENT_TYPE_LICENSE:
          {
            if (LicenseEventHandler != null) LicenseEventHandler(eventId);
            break;
          }
        case TEventType.EVENT_TYPE_ENTITY:
          {
            if (EntityEventHandler != null)
            {
              using (var entity = new TGSEntity(GS5_Intf.gsGetEventSource(hEvent)))
                EntityEventHandler(eventId, entity);
            }
            break;
          }
        case TEventType.EVENT_TYPE_USER:
          {
            if (UserEventHandler != null)
            {
              uint evtDataSize = 0;
              IntPtr pEventData = GS5_Intf.gsGetUserEventData(hEvent, out evtDataSize);
              UserEventHandler(eventId, pEventData, evtDataSize);
            }
            break;
          }
      }
    }

    private static void s_monitorCallback(int eventId, TEventHandle hEvent, IntPtr usrData)
    {
      s_core.OnEvent(eventId, hEvent);
    }

    private gs5_monitor_callback _monitor = new gs5_monitor_callback(s_monitorCallback);

    private TGSCore()
    {
      _entityAccessor = new TEntityAccessor(this);
      _varAccessor = new TVarAccessor(this);
      _internalTimer = new TInternalTimer();
      _timeEngine = new TTimeEngine();
      GS5_Intf.gsCreateMonitorEx(_monitor, IntPtr.Zero, "$SDK");
    }

    public static TGSCore getInstance()
    {
      if (TGSCore.s_core == null)
      {
        TGSCore.s_core = new TGSCore();
      }
      return s_core;
    }

    public bool init(string productId, string productLicFile, string licPassword)
    {
      _rc = GS5_Intf.gsInit(productId, productLicFile, licPassword);
      if (_rc == 0)
      {
        _totalEntities = GS5_Intf.gsGetEntityCount();
        return true;
      }

      return false;
    }

    public bool init(string productId, byte[] productLicData, string licPassword)
    {
      _rc = GS5_Intf.gsInit(productId, productLicData, licPassword);
      if (_rc == 0)
      {
        _totalEntities = GS5_Intf.gsGetEntityCount();
        return true;
      }

      return false;
    }

    public void cleanUp()
    {
      GS5_Intf.gsCleanUp();
    }
    //Revoke a single serial number, all those entities previously unlocked by this sn are locked
    public bool revokeSN(string serialNumber)
    {
      return GS5_Intf.gsRevokeSN(TIMEOUT_WAIT_INFINITE, serialNumber);
    }
    /**
      Revoke all serial numbers of an application

      IN: 
		  
      snCompatible: [optional] serial number for pre-5.3.1 compatible usage.

      Before GS5.3.1, the serial number is not kept in local license data, so you must save it elsewhere and provide it as an external parameter.

      For NEW projects created since GS5.3.1, all applied serial numbers are persisted in local license storage so there is no need for a serial number.
      */
    public bool revokeApp(string snCompatible = "")
    {
      return GS5_Intf.gsRevokeApp(TIMEOUT_WAIT_INFINITE, snCompatible);
    }

    public void Dispose()
    {
      cleanUp();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Lock down all entities
    /// </summary>
    public void lockAllEntities()
    {
      for (int i = 0; i < this._totalEntities; i++)
      {
        getEntityByIndex(i).lockLicense();
      }
    }

    ~TGSCore()
    {
      cleanUp();
    }
    /** \brief Convert event id to human readable string, for debug purpose
    *
    * It can be used to display user-friendly debug message to log view.
    *
    * ref: \ref eventId "Event Id"
    */
    public static string getEventName(uint eventId)
    {
      if (eventId >= (uint)TEventType.EVENT_TYPE_USER)
      {
        return string.Format("User Event({0})", eventId);
      }
      else return ((TEventType)eventId).ToString();
    }
    //Save license immediately if dirty
    public void flush() { GS5_Intf.gsFlush(); }

    public TGSEntity getEntityByIndex(int index)
    {
      if ((index < 0) || (index >= _totalEntities))
        gs5_error.raise(TGSError.GS_ERROR_INVALID_INDEX, "Index [{0}] out of range [0, {1})", index, _totalEntities);

      return new TGSEntity(GS5_Intf.gsOpenEntityByIndex(index));
    }

    public TGSEntity getEntityById(entity_id_t entityId)
    {
      gs_handle_t h = GS5_Intf.gsOpenEntityById(entityId);
      if (h == GS5_Intf.INVALID_GS_HANDLE)
        gs5_error.raise(TGSError.GS_ERROR_INVALID_ENTITY, "Invalid EntityId ({0})", entityId);

      return new TGSEntity(h);
    }

    //Variables
    public TGSVariable addVariable(string varName, TVarType varType, uint varAttr, string initValStr)
    {
      return new TGSVariable(GS5_Intf.gsAddVariable(varName, varType, varAttr, initValStr));
    }

    public bool removeVariable(string varName)
    {
      return GS5_Intf.gsRemoveVariable(varName);
    }
    /// Get user defined variable by its name
    public TGSVariable getVariableByName(string name)
    {
      gs_handle_t h = GS5_Intf.gsGetVariable(name);
      if (h == GS5_Intf.INVALID_GS_HANDLE)
        gs5_error.raise(TGSError.GS_ERROR_INVALID_NAME, "Invalid Variable Name [{0}]", name);

      return new TGSVariable(h);
    }
    /// Get total number of user defined variables
    public int getTotalVariables()
    {
      return GS5_Intf.gsGetTotalVariables();
    }
    /// Get user defined variable by its index
    public TGSVariable getVariableByIndex(int index)
    {
      return new TGSVariable(GS5_Intf.gsGetVariableByIndex(index));
    }

    //Request
    public TGSRequest createRequest()
    {
      return new TGSRequest(GS5_Intf.gsCreateRequest());
    }

    public bool applyLicenseCode(string code, string serial = null)
    {
      return GS5_Intf.gsApplyLicenseCodeEx(code, serial, null);
    }

    public bool applySerialNumber(string sn) //backward compatibility
    {
      return applySN(sn);
    }
    public bool applySN(string sn)
    {
#if ONLINE_ACTIVATION_DOTNET
            try
            {
                string requestCode = getDummyRequestCode();

                CheckPointServiceManager mgr = new CheckPointServiceManager();
                CheckPointServiceManager.CheckPointJSONResponse response = mgr.ActivateProduct(sn, requestCode, this.ProductId, this.BuildId);

                if (response != null)
                {
                    if (!string.IsNullOrEmpty(response.Result))
                    {
                        return this.applyLicenseCode(response.Result);
                    }
                    else
                    {
                        //-2: Fails to activate
                        GS5_Intf.gsSetLastErrorInfo(-2, response.ResponseCode.ToString());  
                    }
                }
            }
            catch (Exception ex)
            {
                // -1: Unknown exception
                GS5_Intf.gsSetLastErrorInfo(-1, ex.Message);
            }

            return false;
#else

      int retCode;
      string snRef;
      return GS5_Intf.gsApplySN(sn, out retCode, out snRef);
#endif
    }

    ///Test if a sn exists and not deleted at server side
    public bool isSNValid(string sn)
    {
      return GS5_Intf.gsIsSNValid(sn, -1);
    }

    ///Test if the CheckPoint license server is alive
    public bool isServerAlive()
    {
      return GS5_Intf.gsIsServerAlive(-1);
    }

    //Get the prelimitary serial number which is used for license transfer (revoke & move) and error fix
    public string getPreliminarySN()
    {
      return GS5_Intf.gsGetPreliminarySN();
    }

    //-------- HTML Render -----------
    public static bool renderHTML(string url, string title, int width, int height)
    {
      return GS5_Intf.gsRenderHTML(url, title, width, height);
    }
    public static bool renderHTML(string url, string title, int width, int height,
        bool resizable, bool exitAppWhenUIClosed, bool cleanUpAfterRendering)
    {
      return GS5_Intf.gsRenderHTMLEx(url, title, width, height, resizable, exitAppWhenUIClosed, cleanUpAfterRendering);
    }

    /** Commonly used request code helpers */
    /// Get request code to unlock the whole application ( all entities/licenses )
    public string getUnlockRequestCode()
    {
      TGSRequest req = this.createRequest();
      req.addAction(TGSAction.ACT_UNLOCK);
      return req.Code;
    }

    public string UnlockRequestCode
    {
      get
      {
        return getUnlockRequestCode();
      }
    }
    /// Get request code to fix the local license error
    public string getFixRequestCode()
    {
      TGSRequest req = this.createRequest();
      req.addAction(TGSAction.ACT_FIX);
      return req.Code;
    }

    public string FixRequestCode
    {
      get
      {
        return getFixRequestCode();
      }
    }
    /// Get request code to clean up the local license
    public string getCleanRequestCode()
    {
      TGSRequest req = this.createRequest();
      req.addAction(TGSAction.ACT_CLEAN);
      return req.Code;
    }

    public string CleanERequestCode
    {
      get
      {
        return getCleanRequestCode();
      }
    }
    /// Get request code to send client information (fingerprint) to server
    public string getDummyRequestCode()
    {
      TGSRequest req = this.createRequest();
      req.addAction(TGSAction.ACT_DUMMY);
      return req.Code;
    }

    public string DummyRequestCode
    {
      get
      {
        return getDummyRequestCode();
      }
    }
    //-------- Debug Helpers ----------
    public static bool isDebugVersion()
    {
      return GS5_Intf.gsIsDebugVersion();
    }
    public void trace(string msg)
    {
      GS5_Intf.gsTrace(msg);
    }
    //======================================================
    //---------- Time Engine Service ------------
    public TInternalTimer InternalTimer
    {
      get { return _internalTimer; }
    }

    public TTimeEngine TimeEngine
    {
      get { return _timeEngine; }
    }

    public int ReturnCode
    {
      get { return _rc; }
    }

    public string LastErrorMessage
    {
      get
      {
        return GS5_Intf.gsGetLastErrorMessage();
      }
    }

    public int LastErrorCode
    {
      get
      {
        return GS5_Intf.gsGetLastErrorCode();
      }
    }

    public string SDKVersion
    {
      get { return GS5_Intf.gsGetVersion(); }
    }

    public string ProductName
    {
      get { return GS5_Intf.gsGetProductName(); }
    }

    public string ProductId
    {
      get { return GS5_Intf.gsGetProductId(); }
    }

    public int BuildId
    {
      get { return GS5_Intf.gsGetBuildId(); }
    }

    public TRunMode RunMode
    {
      get { return GS5_Intf.gsRunInWrappedMode() ? TRunMode.RM_WRAP : TRunMode.RM_SDK; }
    }

    public bool RunInVM
    {
      get { return GS5_Intf.gsRunInsideVM(0xFFFFFFFF); }
    }

    public int EntityCount { get { return _totalEntities; } }

    public TEntityAccessor Entities
    {
      get { return _entityAccessor; }
    }

    public TVarAccessor Variables
    {
      get { return _varAccessor; }
    }

    public bool isAppFirstLaunched()
    {
      return GS5_Intf.gsIsAppFirstLaunched();
    }
    /** \brief Create a new move package
    *
    *  \param mpDataStr the encrypted data string of a move package.
    *         if mpDataStr == NULL, then an empty move package is created
    */
    public void f(){}


    /// <summary>
    /// Create or Open a move package
    /// </summary>
    /// <param name="mpDataStr">Exported package data string
    /// If it is empty, this api creates a new move package instance, otherwise the returned move package has loaded the data string correctly
    /// </param>
    /// <returns></returns>
    public TMovePackage createMovePackage(string mpDataStr = ""){
      TMPHandle hMP = (mpDataStr == "") ? GS5_Intf.gsMPCreate(0) : GS5_Intf.gsMPOpen(mpDataStr);
      if(hMP != IntPtr.Zero) return new TMovePackage(hMP);
      throw new Exception("Cannot create a move package, input package data might be corrupted!");
    }

    /// <summary>
    /// Move the whole license via online license server
    /// </summary>
    /// <param name="preSN"></param>
    /// <returns> on success, a non-empty receipt (SN) to activate app later on target machine </returns>
    public string uploadApp(string preSN = ""){
      //make sure we have a valid preliminary serial number for online operation
      Debug.Assert(preSN != "" || GS5_Intf.gsMPCanPreliminarySNResolved(IntPtr.Zero));

      return GS5_Intf.gsMPUploadApp(preSN, (int)TCheckPointServerTimeout.TIMEOUT_WAIT_INFINITE );
    }

    /// <summary>
    /// Move the whole license manually / offline
    /// </summary>
    /// <returns>Return: on success, a non-empty encrypted string contains the current license data.</returns>
    public string exportApp(){
      return GS5_Intf.gsMPExportApp();
    }

    //Code Exchange
    public static TCodeExchange beginCodeExchange(){
      TCodeExchangeHandle h = GS5_Intf.gsCodeExchangeBegin();
      if (h != IntPtr.Zero) return new TCodeExchange(h);
      throw new Exception("Cannot create a code-exchange instance!");
    }
  };
  #endregion

}

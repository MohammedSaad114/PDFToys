

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0628 */
/* at Tue Jan 19 04:14:07 2038
 */
/* Compiler settings for PDFToysShellExtension.idl:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.01.0628 
    protocol : all , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */



/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 500
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif /* __RPCNDR_H_VERSION__ */

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __PDFToysShellExtension_i_h__
#define __PDFToysShellExtension_i_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

#ifndef DECLSPEC_XFGVIRT
#if defined(_CONTROL_FLOW_GUARD_XFG)
#define DECLSPEC_XFGVIRT(base, func) __declspec(xfg_virtual(base, func))
#else
#define DECLSPEC_XFGVIRT(base, func)
#endif
#endif

/* Forward Declarations */ 

#ifndef __IPDFContextMenu_FWD_DEFINED__
#define __IPDFContextMenu_FWD_DEFINED__
typedef interface IPDFContextMenu IPDFContextMenu;

#endif 	/* __IPDFContextMenu_FWD_DEFINED__ */


#ifndef __PDFContextMenu_FWD_DEFINED__
#define __PDFContextMenu_FWD_DEFINED__

#ifdef __cplusplus
typedef class PDFContextMenu PDFContextMenu;
#else
typedef struct PDFContextMenu PDFContextMenu;
#endif /* __cplusplus */

#endif 	/* __PDFContextMenu_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "shobjidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __IPDFContextMenu_INTERFACE_DEFINED__
#define __IPDFContextMenu_INTERFACE_DEFINED__

/* interface IPDFContextMenu */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_IPDFContextMenu;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6db695b5-ee46-4e2a-898e-6c95590a92ca")
    IPDFContextMenu : public IUnknown
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct IPDFContextMenuVtbl
    {
        BEGIN_INTERFACE
        
        DECLSPEC_XFGVIRT(IUnknown, QueryInterface)
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IPDFContextMenu * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        DECLSPEC_XFGVIRT(IUnknown, AddRef)
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IPDFContextMenu * This);
        
        DECLSPEC_XFGVIRT(IUnknown, Release)
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IPDFContextMenu * This);
        
        END_INTERFACE
    } IPDFContextMenuVtbl;

    interface IPDFContextMenu
    {
        CONST_VTBL struct IPDFContextMenuVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPDFContextMenu_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IPDFContextMenu_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IPDFContextMenu_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IPDFContextMenu_INTERFACE_DEFINED__ */



#ifndef __PDFToysShellExtensionLib_LIBRARY_DEFINED__
#define __PDFToysShellExtensionLib_LIBRARY_DEFINED__

/* library PDFToysShellExtensionLib */
/* [version][uuid] */ 


EXTERN_C const IID LIBID_PDFToysShellExtensionLib;

EXTERN_C const CLSID CLSID_PDFContextMenu;

#ifdef __cplusplus

class DECLSPEC_UUID("2a97e713-bd5f-45c4-a410-e57a3c3d86a4")
PDFContextMenu;
#endif
#endif /* __PDFToysShellExtensionLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif



// dllmain.h : Declaration of module class.

class CPDFToysShellExtensionModule : public ATL::CAtlDllModuleT< CPDFToysShellExtensionModule >
{
public :
	DECLARE_LIBID(LIBID_PDFToysShellExtensionLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_PDFTOYSSHELLEXTENSION, "{1da6b74d-ba29-4d50-9244-89ab06b1aebe}")
};

extern class CPDFToysShellExtensionModule _AtlModule;

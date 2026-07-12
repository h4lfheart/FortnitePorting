import json
from .context import ImportContext
from ..server import Server
from ..utils import addon_version


class Importer:
    _version_checked = False

    @staticmethod
    def Import(data: str):
        json_data = json.loads(data)

        meta = json_data.get("MetaData")
        exports = json_data.get("Exports")

        if not Importer._version_checked:
            Importer._version_checked = True
            Importer._check_version(meta)

        for export in exports:
            context = ImportContext(meta)
            context.run(export)

    @staticmethod
    def _check_version(meta: dict):
        try:
            app_version_str = (meta or {}).get("Version", "").lstrip("v").split("-")[0]
            if not app_version_str:
                return
            app_parts = tuple(int(p) for p in app_version_str.split(".") if p.isdigit())
            meta_version = addon_version()
            if app_parts != meta_version:
                plugin_str = ".".join(str(x) for x in meta_version)
                Server.instance.send_dialog(
                    f"Your Fortnite Porting Blender plugin is out of date.\n"
                    f"The currently installed version is v{plugin_str}, but the latest version available is v{app_version_str}."
            
                )
        except Exception:
            pass

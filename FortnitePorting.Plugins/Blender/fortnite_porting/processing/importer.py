import json
from .import_context import ImportContext


class Importer:
    @staticmethod
    def Import(data: str):
        json_data = json.loads(data)

        meta = json_data.get("MetaData")
        exports = json_data.get("Exports")
        for export in exports:
            context = ImportContext(meta)
            context.run(export)

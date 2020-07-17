import sys, glob, os
import ruamel
from copy import deepcopy
from metafile_parser import *
from jobs.shared.namer import *
from jobs.projects.yml_project import create_project_ymls
from jobs.editor.yml_editor import create_editor_yml
from jobs.packages.yml_package import create_package_ymls
from jobs.abv.yml_abv import create_abv_ymls
from jobs.preview_publish.yml_pb import create_preview_publish_ymls
from jobs.templates.yml_template import create_template_ymls

root_dir = os.path.dirname(os.path.dirname(os.getcwd()))
root_dir = os.getcwd()
yamato_dir = os.path.join(root_dir,'.yamato')
config_dir = os.path.join(yamato_dir,'config')
comment = ''' 
# This file is generated by .yamato/ruamel/build.py. Don't edit this file manually. 
# Introduce any changes under .yamato/config/*.metafile files (for most cases) or under .yamato/ruamel/* within Python (more elaborate cases), and rerun build.py to regenerate all .yml files. 
# Read more under .yamato/docs/readme.md 
\n'''

shared = {}
latest_editor_versions = {}
yml_files = {}

def yml_load(filepath):
    with open(filepath) as f:
        return yaml.load(f)

def yml_dump_files(yml_file_dict):
    for filepath,yml_dict in yml_file_dict.items():
        with open(os.path.join(root_dir,filepath), 'w') as f:
            yaml.dump(yml_dict, f)
        yml_files[filepath.split('/')[-1]] = {'path':filepath, 'yml':yml_dict}


def assert_dependencies():
    for yml_file, yml_value in yml_files.items():
        yml_content = yml_value['yml']
        for job_id, job_content in yml_content.items():
            for dependency in job_content.get('dependencies') or []:
                dep_path = (dependency if  isinstance(dependency, str) else dependency['path']).split('/')[1]
                dep_file, dep_job_id = dep_path.split('#')[0], dep_path.split('#')[1]
                try:
                    assert yml_files[dep_file]['yml'][dep_job_id] 
                except:
                    print(f'Mistake in file {yml_file}#{job_id} for dependency {dep_file}#{dep_job_id}')


def add_comments():
    for yml_file, yml_value in yml_files.items():
        with open(os.path.join(root_dir,yml_value['path']), 'r+') as f:
            yml = f.read()
            f.seek(0, 0)
            f.write(comment)
            f.write(yml)

def get_metafile(metafile_name, unfold_agents_root_keys=[], unfold_test_platforms_root_keys=[]):
    metafile = yml_load(metafile_name)
    return format_metafile(metafile, shared, latest_editor_versions, unfold_agents_root_keys, unfold_test_platforms_root_keys)


if __name__== "__main__":

    # configure yaml
    yaml = ruamel.yaml.YAML()
    yaml.width = 4096
    yaml.indent(offset=2, mapping=4, sequence=5)

    # clear directory from existing yml files, not to have old duplicates etc
    old_yml_files = glob.glob(os.path.join(yamato_dir,'*.yml'), recursive=True)
    for f in old_yml_files:
        os.remove(f)

    # read shared file
    shared = yml_load(os.path.join(config_dir,'__shared.metafile'))
    latest_editor_versions = yml_load(os.path.join(config_dir,'_latest_editor_versions.metafile'))

    # create editor
    print(f'Running: editor')
    editor_metafile = get_metafile(os.path.join(config_dir,'_editor.metafile'))
    yml_dump_files(create_editor_yml(editor_metafile))

    # create package jobs
    print(f'Running: packages')
    package_metafile = get_metafile(os.path.join(config_dir,'_packages.metafile'))
    yml_dump_files(create_package_ymls(package_metafile))

    # create abv
    abv_metafile = get_metafile(os.path.join(config_dir,'_abv.metafile'), unfold_agents_root_keys=['smoke_test'], unfold_test_platforms_root_keys=['smoke_test'])
    yml_dump_files(create_abv_ymls(abv_metafile))

    # create preview publish
    print(f'Running: preview_publish')
    pb_metafile = get_metafile(os.path.join(config_dir,'_preview_publish.metafile'))
    yml_dump_files(create_preview_publish_ymls(pb_metafile))

    # create template jobs
    print(f'Running: templates')
    template_metafile = get_metafile(os.path.join(config_dir,'_templates.metafile'))
    yml_dump_files(create_template_ymls(template_metafile))

    # create yml jobs for each specified project
    #for project_metafile in glob.glob(os.path.join(config_dir,'universal.metafile')):
    for project_metafile in glob.glob(os.path.join(config_dir,'[!_]*.metafile')):
        print(f'Running: {project_metafile}')   
        project_metafile = get_metafile(project_metafile)
        yml_dump_files(create_project_ymls(project_metafile))
        
    # # running assert checks for dependency paths
    print(f'Checking dependency paths')
    assert_dependencies()

    # # add comments on top of all yml files
    print(f'Adding comments')
    add_comments()

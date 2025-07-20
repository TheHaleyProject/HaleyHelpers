CREATE SCHEMA dss_core;

CREATE  TABLE dss_core.client ( 
	id                   INT    NOT NULL AUTO_INCREMENT  PRIMARY KEY,
	name                 VARCHAR(100)    NOT NULL   ,
	fullname             VARCHAR(240)    NOT NULL   ,
	managed_name         VARCHAR(140)    NOT NULL   ,
	guid                 VARCHAR(48)  DEFAULT uuid()  NOT NULL   ,
	path                 VARCHAR(140)    NOT NULL   ,
	created_on           DATETIME  DEFAULT current_timestamp()  NOT NULL   ,
	notes                VARCHAR(600)       ,
	CONSTRAINT unq_client UNIQUE ( name ) ,
	CONSTRAINT unq_client_0 UNIQUE ( managed_name ) ,
	CONSTRAINT unq_client_1 UNIQUE ( guid ) 
 ) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

CREATE  TABLE dss_core.client_suffix ( 
	id                   INT    NOT NULL   PRIMARY KEY,
	file                 VARCHAR(1)  DEFAULT 'f'  NOT NULL   ,
	directory            VARCHAR(1)  DEFAULT 'd'  NOT NULL   ,
	CONSTRAINT fk_client_suffix_client FOREIGN KEY ( id ) REFERENCES dss_core.client( id ) ON DELETE NO ACTION ON UPDATE NO ACTION
 ) engine=InnoDB;

CREATE  TABLE dss_core.tag ( 
	id                   INT    NOT NULL AUTO_INCREMENT  PRIMARY KEY,
	name                 VARCHAR(100)    NOT NULL   ,
	CONSTRAINT unq_tag UNIQUE ( name ) 
 ) engine=InnoDB;

CREATE  TABLE dss_core.directory ( 
	parent               INT    NOT NULL   ,
	id                   BIGINT    NOT NULL AUTO_INCREMENT  PRIMARY KEY,
	name                 VARCHAR(120)    NOT NULL   ,
	managed_name         VARCHAR(140)    NOT NULL   ,
	path                 VARCHAR(140)       ,
	created_on           DATETIME  DEFAULT current_timestamp()  NOT NULL   ,
	notes                VARCHAR(600)       ,
	type                 INT    NOT NULL   ,
	isactive             BIT  DEFAULT b'1'  NOT NULL   ,
	guid                 VARCHAR(48)  DEFAULT 'uuid()'  NOT NULL   ,
	CONSTRAINT unq_directory UNIQUE ( name ) ,
	CONSTRAINT unq_directory_0 UNIQUE ( managed_name ) ,
	CONSTRAINT unq_directory_1 UNIQUE ( guid ) ,
	CONSTRAINT fk_direcory_client FOREIGN KEY ( parent ) REFERENCES dss_core.client( id ) ON DELETE NO ACTION ON UPDATE NO ACTION,
	CONSTRAINT fk_directory_tag FOREIGN KEY ( type ) REFERENCES dss_core.tag( id ) ON DELETE NO ACTION ON UPDATE NO ACTION
 ) engine=InnoDB;

CREATE INDEX fk_direcory_client ON dss_core.directory ( parent );

CREATE INDEX idx_directory ON dss_core.directory ( parent, name );

CREATE INDEX idx_directory_0 ON dss_core.directory ( parent, managed_name );

CREATE INDEX idx_directory_1 ON dss_core.directory ( type );

CREATE INDEX idx_directory_2 ON dss_core.directory ( parent, type );

ALTER TABLE dss_core.client MODIFY managed_name VARCHAR(140)  NOT NULL   COMMENT 'managedname is based on the name (not) the fullname';

ALTER TABLE dss_core.client MODIFY guid VARCHAR(48)  NOT NULL DEFAULT uuid()  COMMENT 'this guid is used to create separate schema for all the clients where in the files will be stored.';

ALTER TABLE dss_core.directory MODIFY path VARCHAR(140)     COMMENT 'Path is not mandatory. Sometimes if it is not present, we just assume that the data is present in the base directory level. As file ids are created at client level (and not at directory level), not having a path will not create any conflicts. Only file ids will not create issues. However, storing the file itself might create conflicts. So need to be careful with that approach.';


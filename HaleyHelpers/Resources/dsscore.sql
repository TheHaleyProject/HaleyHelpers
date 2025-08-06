-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               11.7.2-MariaDB - mariadb.org binary distribution
-- Server OS:                    Win64
-- HeidiSQL Version:             12.7.0.6850
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dumping database structure for dss_core
CREATE DATABASE IF NOT EXISTS `dss_core` /*!40100 DEFAULT CHARACTER SET latin1 COLLATE latin1_swedish_ci */;
USE `dss_core`;

-- Dumping structure for table dss_core.client
CREATE TABLE IF NOT EXISTS `client` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `display_name` varchar(100) NOT NULL,
  `guid` varchar(48) NOT NULL DEFAULT uuid(),
  `path` varchar(140) NOT NULL COMMENT 'Created only at register time.\nWe would have anyhow created the guid based on the provided name. If the client is created as managed, then the path should be based on the guid. or else it should be based on the name itself.',
  `created` timestamp NOT NULL DEFAULT current_timestamp(),
  `modified` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_client` (`name`),
  UNIQUE KEY `unq_client_1` (`guid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_core.client_keys
CREATE TABLE IF NOT EXISTS `client_keys` (
  `client` int(11) NOT NULL,
  `signing` varchar(300) NOT NULL,
  `encrypt` varchar(300) NOT NULL,
  `password` varchar(120) NOT NULL,
  PRIMARY KEY (`client`),
  CONSTRAINT `fk_client_keys_client` FOREIGN KEY (`client`) REFERENCES `client` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_core.module
CREATE TABLE IF NOT EXISTS `module` (
  `parent` int(11) NOT NULL,
  `id` int(11) NOT NULL,
  `name` varchar(120) NOT NULL,
  `display_name` varchar(120) NOT NULL,
  `guid` varchar(48) NOT NULL,
  `path` varchar(200) NOT NULL,
  `created` datetime NOT NULL DEFAULT current_timestamp(),
  `active` bit(1) NOT NULL DEFAULT b'1',
  `modified` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `cuid` varchar(48) NOT NULL COMMENT 'collision resistant unique identifier',
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_directory_01` (`parent`,`name`),
  UNIQUE KEY `unq_module` (`parent`,`guid`),
  UNIQUE KEY `unq_module_0` (`cuid`),
  CONSTRAINT `fk_direcory_client` FOREIGN KEY (`parent`) REFERENCES `client` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_core.workspace
CREATE TABLE IF NOT EXISTS `workspace` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `parent` int(11) NOT NULL,
  `name` varchar(120) NOT NULL,
  `display_name` varchar(120) NOT NULL,
  `guid` varchar(48) NOT NULL,
  `path` varchar(200) NOT NULL,
  `active` bit(1) NOT NULL DEFAULT b'1',
  `created` timestamp NOT NULL DEFAULT current_timestamp(),
  `modified` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `control_mode` int(11) NOT NULL DEFAULT 0 COMMENT '0 - none\n1 - numbers\n2 - hash\n3 - both',
  `parse_mode` int(11) NOT NULL DEFAULT 0 COMMENT '0- Parse\n1- Generate\n2- Parse or generate',
  `cuid` varchar(48) NOT NULL COMMENT 'collision resistant unique identifier',
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_workspace` (`parent`,`name`),
  UNIQUE KEY `unq_workspace_0` (`parent`,`guid`),
  UNIQUE KEY `unq_workspace_1` (`cuid`),
  CONSTRAINT `fk_workspace_module` FOREIGN KEY (`parent`) REFERENCES `module` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `cns_workspace` CHECK (`control_mode` >= 0 and `control_mode` <= 3),
  CONSTRAINT `cns_workspace_0` CHECK (`parse_mode` >= 0 and `parse_mode` <= 2)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
